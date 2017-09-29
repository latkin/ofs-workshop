namespace OFS.Comments

open System

// simple comment representation
type Comment =
    { time : DateTimeOffset
      name : string
      comment : string }
