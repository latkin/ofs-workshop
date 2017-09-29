namespace OFS.Comments

open System
open Ganss.XSS // need to add ref in project.json "HtmlSanitizer": "3.4.156"
open Markdig // need to add ref in project.json "MarkDig": "0.11.0"
open Microsoft.Azure.WebJobs.Host
open System.Web

exception ProcessingExn of string

module Processing =
    open System.Net.Http
    open Newtonsoft.Json

    type ReCaptchaResponse =
        { success : bool
          challenge_ts : string
          hostname : string }

    let private bodySanitizer = HtmlSanitizer()
    let private nameSanitizer = HtmlSanitizer(allowedTags = [])
    let private mdPipeline =
        MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseEmojiAndSmiley()
            .Build()

    let private sanitizeBody str = bodySanitizer.Sanitize(str)
    let private sanitizeName str = nameSanitizer.Sanitize(str)
    let private markdownToHtml str = Markdown.ToHtml(str, mdPipeline)

    let private getRemoteIp (req : HttpRequestMessage) =
        match req.Properties.TryGetValue("MS_HttpContext") with
        | (true, prop) -> Some((prop :?>  HttpContextWrapper).Request.UserHostAddress)
        | _ ->
            match HttpContext.Current with
            | null -> None
            | current -> Some(current.Request.UserHostAddress)
    
    // check that the provided captcha token is valid
    let private checkCaptcha (log: TraceWriter) (req : HttpRequestMessage) recaptchaSecret token =
        async {
            let remoteIp = defaultArg (getRemoteIp req) ""
            let client = new HttpClient()
            let! response =
                client.GetAsync(
                    sprintf "https://www.google.com/recaptcha/api/siteverify?secret=%s&remoteip=%s&response=%s"
                        recaptchaSecret
                        remoteIp
                        token) |> Async.AwaitTask
            let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            log.Info(sprintf "Raw captcha response: %s" content)

            let captchaResult = JsonConvert.DeserializeObject<ReCaptchaResponse>(content)
            if captchaResult.success then return ()
            else raise (ProcessingExn("captcha failed"))
        }

    let private check name maxLen str =
        if String.IsNullOrWhiteSpace(str) || str.Length > maxLen then
            raise (ProcessingExn(sprintf "Invalid value for %s" name))
        else str
    
    // implements processing step from raw user input to
    // fully pre-processed comment
    let userCommentToPending (log: TraceWriter) (req : HttpRequestMessage) (settings : Settings) (userComment : UserProvidedComment) : Async<PendingComment> =
        async {
            do! checkCaptcha log req settings.ReCaptchaSecret userComment.captcha

            let finalName =
                userComment.name
                |> sanitizeName
                |> check "name" 100

            log.Info(sprintf "Original comment name: %s" userComment.name)
            log.Info(sprintf "Final comment name: %s" finalName)

            let htmlComment =
                userComment.comment
                |> markdownToHtml
                |> sanitizeBody
                |> check "comment" 2048

            log.Info(sprintf "Original comment body: %s" userComment.comment)
            log.Info(sprintf "Final comment body: %s" htmlComment)

            return { postid = userComment.postid
                     name = finalName
                     commentHtml = htmlComment
                     commentRaw = userComment.comment
                     createdAt = DateTimeOffset.UtcNow }
        }
