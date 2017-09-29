#r "System.Net.Http"
#r "Newtonsoft.Json"
#load "CommentTypes.fs"

#if INTERACTIVE
#r "../packages/Microsoft.Azure.WebJobs.2.0.0/lib/net45/Microsoft.Azure.WebJobs.Host.dll"
#r "../packages/System.Net.Http.Formatting.Extension.5.2.3.0/lib/System.Net.Http.Formatting.dll"
#r "../packages/System.Web.Http.4.0.0/System.Web.Http.dll"
#r "../packages/Newtonsoft.Json.10.0.3/lib/net45/Newtonsoft.Json.dll"
#endif

open System.Net
open System.Net.Http
open Newtonsoft.Json

open System
open System.Collections.Generic
open Microsoft.Azure.WebJobs.Host

open OFS.Comments

// define this since Azure Functions aren't running F# 4.1
module Option = let defaultValue x = function None -> x | Some(v) -> v

type CommentRequest =
    | GetComments of postId : string
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
        else
            Invalid(sprintf "Unsupported HTTP method %O" req.Method)

let Run(req: HttpRequestMessage, log: TraceWriter) =
    async {
        try
            match CommentRequest.init req with
            | GetComments(postId) ->
                log.Info(sprintf "Request to get comments for post %s" postId)

                // just return some dummy data
                let comments = [|
                    { time = DateTimeOffset.UtcNow.AddHours(-2.)
                      name = "Sue"
                      comment = "abc" }
                    { time = DateTimeOffset.UtcNow.AddHours(-1.)
                      name = "Joe"
                      comment = "123" }
                    { time = DateTimeOffset.UtcNow
                      name = "Rex"
                      comment = "xyz" }
                |]

                log.Info(sprintf "Loaded %d comments for post %s" comments.Length postId)

                let response = req.CreateResponse(HttpStatusCode.OK)
                response.Content <- new StringContent(JsonConvert.SerializeObject(comments))
                return response
            | Invalid(msg) ->
                log.Error(sprintf "Invalid request: %s" msg)
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, msg)
        with
        | exn ->
            log.Error("Unknown error", exn)
            return req.CreateErrorResponse(HttpStatusCode.InternalServerError, "Unknown error")
    } |> Async.RunSynchronously
