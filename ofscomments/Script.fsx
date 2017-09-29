#r "System.Net.Http"
#r "Newtonsoft.Json"

#if INTERACTIVE
#r "../packages/Microsoft.Azure.WebJobs.2.0.0/lib/net45/Microsoft.Azure.WebJobs.Host.dll"
#r "../packages/System.Net.Http.Formatting.Extension.5.2.3.0/lib/System.Net.Http.Formatting.dll"
#r "../packages/System.Web.Http.4.0.0/System.Web.Http.dll"
#r "../packages/Newtonsoft.Json.10.0.3/lib/net45/Newtonsoft.Json.dll"
#I "../packages/WindowsAzure.Storage.8.4.0/lib/net45"
#endif

#r "Microsoft.WindowsAzure.Storage"

#load "CommentTypes.fs"
#load "CommentStorage.fs"

open System.Net
open System.Net.Http
open Newtonsoft.Json

open System
open System.Collections.Generic
open Microsoft.Azure.WebJobs.Host

open OFS.Comments
open OFS.Comments.CommentStorage

// define this since Azure Functions aren't running F# 4.1
module Option = let defaultValue x = function None -> x | Some(v) -> v

type CommentRequest =
    | GetComments of postId : string
    | AddComment of comment : UserProvidedComment
    | Invalid of msg : string
    with

    // convert raw HTTP request into appropriate logical action
    // and corresponding data
    static member init (req : HttpRequestMessage) =
        if req.Method = HttpMethod.Get then
            req.GetQueryNameValuePairs()
            |> Seq.tryFind (fun q -> q.Key = "postid")
            |> Option.map (fun kvp -> GetComments(kvp.Value))
            |> Option.defaultValue (Invalid("postid parameter is required"))
        elif req.Method = HttpMethod.Post then
            let content =
                req.Content.ReadAsStringAsync()
                |> Async.AwaitTask
                |> Async.RunSynchronously
            try
                match content with
                | content when String.IsNullOrWhiteSpace(content) ->
                    Invalid("Empty request body")
                | content ->
                    match JsonConvert.DeserializeObject<UserProvidedComment>(content) with
                    | newComment when (box newComment) = null ->
                        Invalid("Unable to deserialize comment")
                    | newComment -> 
                        if String.IsNullOrWhiteSpace(newComment.comment) ||
                            String.IsNullOrWhiteSpace(newComment.name) ||
                            String.IsNullOrWhiteSpace(newComment.postid) then
                                Invalid("All fields must be populated")
                        else
                            AddComment(newComment)
            with
            | :? JsonReaderException -> Invalid("Unable to parse request body")
        else
            Invalid(sprintf "Unsupported HTTP method %O" req.Method)

let okJson (req : HttpRequestMessage) body =
    let response = req.CreateResponse(HttpStatusCode.OK)
    response.Content <- new StringContent(JsonConvert.SerializeObject(body))
    response

let Run(req: HttpRequestMessage, log: TraceWriter) =
    async {
        try
            match CommentRequest.init req with
            | GetComments(postId) ->
                log.Info(sprintf "Request to get comments for post %s" postId)

                // load comments from table storage for the specified post
                let storage = CommentStorage(Settings.load())
                let comments = storage.GetCommentsForPost(postId)

                log.Info(sprintf "Loaded %d comments for post %s" comments.Length postId)

                return (okJson req comments)
            | AddComment(newComment) ->
                log.Info(sprintf "Request to add comment for post %s" newComment.postid)

                // add new comment to table storage and return it back to the user
                let storage = CommentStorage(Settings.load())
                let finalComment = storage.AddCommentForPost(newComment)

                log.Info(sprintf "Successfully added new comment to post %s" newComment.postid)

                return (okJson req finalComment)
            | Invalid(msg) ->
                log.Error(sprintf "Invalid request: %s" msg)
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, msg)
        with
        | exn ->
            log.Error("Unknown error", exn)
            return req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Unknown error")
    } |> Async.RunSynchronously
