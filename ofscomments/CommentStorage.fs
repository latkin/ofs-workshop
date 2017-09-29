namespace OFS.Comments

open System
open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Table

type Settings =
    { StorageConnectionString : string
      TableName : string 
      ReCaptchaSecret : string }
    with

    // grab various settings from app settings, exposed as env vars
    static member load () = 
        { StorageConnectionString =
            Environment.GetEnvironmentVariable("APPSETTING_ofs_comments_connectionstring")
          TableName =
            Environment.GetEnvironmentVariable("APPSETTING_ofs_comments_tablename")
          ReCaptchaSecret =
            Environment.GetEnvironmentVariable("APPSETTING_ofs_comments_recaptchasecret") }

module CommentStorage =

    let private genPartitionKey (postId : string) = 
        let postId =
            postId.Replace('/', '.').Replace('\\','.').Replace('#','.').Replace('?', '.')
        sprintf "post-%s" postId

    type CommentStorage(settings) =
        let table = 
                CloudStorageAccount.Parse(settings.StorageConnectionString)
                    .CreateCloudTableClient()
                    .GetTableReference(settings.TableName)

        let commentFromRow (row: TableRowComment) =
            { time = row.CreatedAt
              name = row.Name
              comment = row.HtmlComment }

        // looks up all comments for the given post ID from table storage,
        // and returns them sorted by time, in canonical form
        member __.GetCommentsForPost(postId) =
            let postKey = genPartitionKey postId
            let q =
                TableQuery<TableRowComment>()
                    .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, postKey))
            table.ExecuteQuery(q)
            |> Seq.map commentFromRow
            |> Array.ofSeq
            |> Array.sortBy (fun c -> c.time)

        // takes a pending comment and adds it to table storage,
        // returning the final canonical version
        member __.AddCommentForPost(comment: PendingComment) =
            let commentRow = TableRowComment(PartitionKey = (genPartitionKey comment.postid),
                                        RowKey = Guid.NewGuid().ToString(),
                                        Name = comment.name,
                                        RawComment = comment.commentRaw,
                                        HtmlComment = comment.commentHtml,
                                        CreatedAt = DateTimeOffset.UtcNow)

            let result = table.Execute(TableOperation.Insert(commentRow))
            (result.Result :?> TableRowComment) |> commentFromRow

