#load @"global_data.fs"
#load @"user.fs"
#load @"server.fs"
#load @"tweet.fs"
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 

// open GlobalDataMod
// open TweetMod
open UserMod
open Server
open System
open Akka.Actor
open Akka.FSharp
// open Akka.Configuration

// let instance = User(69)
// instance.AddToFollowers ["loda";"lassan"] 
// instance.AddToTimeline ["lod";"assan"] 

let system = ActorSystem.Create("Twitter")
let mutable numNodes:int = 0
let mutable numTweets:int = 0
printfn "UserId: %A" instance.Id


let main(args: array<string>) =
    // if args.Length < 1 then
        
    let mutable actor = spawn system "server" Server
    // else
    //     //spawn client
    let n,r = int(args.[3]),int(args.[4])
    let mutable errorFlag = false
    numNodes <-n
    numTweets <-r

main(Environment.GetCommandLineArgs())
System.Console.ReadKey() |> ignore
