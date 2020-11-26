module TweetMod

type Tweet(tweetid: int, creator :string, content: string) = 
    let id = tweetid
    let mutable mentions: List<string> = List.empty<string>
    // let mutable hast: List<string> = List.empty<string>

    member this.Created = System.DateTime.Now
    member this.Content = content
    member this.Creator = creator