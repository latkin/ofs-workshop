namespace OFS.Comments

open System
open Microsoft.WindowsAzure.Storage.Table

// simple comment representation
type Comment =
    { time : DateTimeOffset
      name : string
      comment : string }

// unsanitized raw form of comment, as submitted by users
type UserProvidedComment = 
    { postid : string
      name : string
      comment : string }

// sanitized + pre-processed comment, ready for storage
type PendingComment = 
    { postid: string
      name : string
      commentHtml : string
      commentRaw : string 
      createdAt : DateTimeOffset }

// comment implemented as TableEntity for interaction with
// table storage
type TableRowComment() =
    inherit TableEntity()
    member val Name = "" with get,set
    member val RawComment = "" with get,set
    member val HtmlComment = "" with get,set
    member val CreatedAt = DateTimeOffset.MinValue with get,set
