namespace OFS.Comments

open System
open Microsoft.WindowsAzure.Storage.Table

// simple comment representation
type Comment =
    { time : DateTimeOffset
      name : string
      comment : string }

// comment implemented as TableEntity for interaction with
// table storage
type TableRowComment() =
    inherit TableEntity()
    member val Name = "" with get,set
    member val Comment = "" with get,set
    member val CreatedAt = DateTimeOffset.MinValue with get,set
