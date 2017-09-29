namespace OFS.Comments

open System
open Ganss.XSS // need to add ref in project.json "HtmlSanitizer": "3.4.156"
open Microsoft.Azure.WebJobs.Host

exception ProcessingExn of string

module Processing =
    open System.Net.Http
    open Newtonsoft.Json

    let private bodySanitizer = HtmlSanitizer()
    let private nameSanitizer = HtmlSanitizer(allowedTags = [])

    let private sanitizeBody str = bodySanitizer.Sanitize(str)
    let private sanitizeName str = nameSanitizer.Sanitize(str)

    let private check name maxLen str =
        if String.IsNullOrWhiteSpace(str) || str.Length > maxLen then
            raise (ProcessingExn(sprintf "Invalid value for %s" name))
        else str
    
    // implements processing step from raw user input to
    // fully pre-processed comment
    let userCommentToPending (log: TraceWriter) (userComment : UserProvidedComment) =
        let finalName =
            userComment.name
            |> sanitizeName
            |> check "name" 100

        log.Info(sprintf "Original comment name: %s" userComment.name)
        log.Info(sprintf "Final comment name: %s" finalName)

        let htmlComment =
            userComment.comment
            |> sanitizeBody
            |> check "comment" 2048

        log.Info(sprintf "Original comment body: %s" userComment.comment)
        log.Info(sprintf "Final comment body: %s" htmlComment)

        { postid = userComment.postid
          name = finalName
          commentHtml = htmlComment
          commentRaw = userComment.comment
          createdAt = DateTimeOffset.UtcNow }
