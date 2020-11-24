module UserMod

type User(uId) = 
    // private immutable value

    let created = 0
    // private mutable value
    let mutable followers: List<string> = List.empty

    let mutable tweets: List<string> = List.empty

    let mutable timeLine: List<string> = List.empty

    member this.Followers
        with get() = followers
    
    member this.Timeline
        with get() = timeLine
    
    member this.Tweets
        with get() = tweets
    

    member this.Id = uId

    // private function definition
    member this.AddToFollowers input = 
        followers <- followers @ input
 
    // private function definition
    member this.AddToTimeline input = 
        timeLine <- timeLine @ input

    member this.AddToTweets input = 
        tweets <- tweets @ input

// test
let instance = User(69)
instance.AddToFollowers ["loda";"lassan"] 
instance.AddToTimeline ["lod";"assan"] 


