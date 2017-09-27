#r "System.Net.Http"
#r "Newtonsoft.Json"
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

type Named = {
    name: string
}

let Run(req: HttpRequestMessage, log: TraceWriter) =
    async {
        log.Info(sprintf 
            "F# HTTP trigger function processed a request.")

        // Set name to query string
        let name =
            req.GetQueryNameValuePairs()
            |> Seq.tryFind (fun q -> q.Key = "name")

        match name with
        | Some x ->
            return req.CreateResponse(HttpStatusCode.OK, "Hello " + x.Value);
        | None ->
            let! data = req.Content.ReadAsStringAsync() |> Async.AwaitTask

            if not (String.IsNullOrEmpty(data)) then
                let named = JsonConvert.DeserializeObject<Named>(data)
                return req.CreateResponse(HttpStatusCode.OK, "Hello " + named.name);
            else
                return req.CreateResponse(HttpStatusCode.BadRequest, "Specify a Name value");
    } |> Async.RunSynchronously
