#load @"global_data.fs"
#load @"user.fs"
#load @"tweet.fs"

open GlobalDataMod
open TweetMod
open UserMod

let instance = User(69)
instance.AddToFollowers ["loda";"lassan"] 
instance.AddToTimeline ["lod";"assan"] 

printfn "UserId: %A" instance.Id
