module TweetMod
open System.Text.RegularExpressions;

type Tweet(tweetid: int, creator :string, content: string) = 
    // do this.MentionsNHashtags()
    // member this.MentionsNHashtags() =
    //     printfn "hastags %A" this.Hastags; 
    //     printfn "mentions %A" this.Mentions; 

    member this.Id = tweetid
    member this.Created = System.DateTime.Now
    member this.Content = content
    member this.Creator = creator
    member this.Mentions: List<string> = [for x in content.Split [|' '|] do if x.StartsWith('@') && x.Length>1  then yield x]
    member this.Hashtags: List<string> = [for x in content.Split [|' '|] do if x.StartsWith('#') && x.Length>1  then yield x]
