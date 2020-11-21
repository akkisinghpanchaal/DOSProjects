open System

type Tweet(content: string, creator: int) = 
    
    let mutable mentions: List<string> = List.empty<string>
    
    member this.Created = System.DateTime.Now
    member this.Content = content
    member this.Creator = creator