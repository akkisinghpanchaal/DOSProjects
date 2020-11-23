open System

type User(uId,followers,tweets,timeLine) = 
    // private immutable value
    let userId = uId

    let created = 0
    // private mutable value
    let mutable followers = [||]

    let mutable tweets = [||]

    let mutable timeLine = [||]

    // private function definition
    member this.AddToFollowers input = 
        followers <- followers + input
 
    // private function definition
    member this.AddToTimeline input = 
        timeLine <- timeLine + input

    member this.AddToTweets input = 
        tweets <- tweets + input

// test
let instance = User(42,[||],[||]a,[||])
// instance.AddToFollowers [|2|]
// instance.AddToTimeline 43


