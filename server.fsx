open System
open System.Net
open System.IO

let url = "http://localhost:8080/"
let listener = new HttpListener()
listener.Prefixes.Add(url)
listener.Start()

printfn "Listening at %s..." url

let rec loop () =
    if not listener.IsListening then ()

    let context = listener.GetContext()
    let requestUrl = context.Request.Url
    let response = context.Response

    printfn "\n%s > %O" (DateTime.Now.ToString("HH:mm:ss.fff")) requestUrl

    match requestUrl.LocalPath with
    | null
    | "" ->
        response.StatusCode <- 404
    | localPath ->
        let fullDiskPath = Environment.CurrentDirectory + localPath
        if File.Exists(fullDiskPath) then
          let buffer = File.ReadAllBytes(fullDiskPath)
          response.ContentLength64 <- int64 buffer.Length
          response.OutputStream.Write(buffer, 0, buffer.Length)
        else
            response.StatusCode <- 404

    response.Close()
    printfn "%s < %d" (DateTime.Now.ToString("HH:mm:ss.fff")) response.StatusCode
    loop ()

loop () |> ignore