#load @"global_data.fs"
#load @"user.fs"
#load @"server.fs"
#load @"tweet.fs"
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 

open GlobalDataMod
open TweetMod
open UserMod
open System
open Akka.Actor
open Akka.FSharp
open Akka.Configuration

// let instance = User(69)
// instance.AddToFollowers ["loda";"lassan"] 
// instance.AddToTimeline ["lod";"assan"] 

let system = ActorSystem.Create("Twitter")
let mutable numNodes:int = 0
let mutable numTweets:int = 0
printfn "UserId: %A" instance.Id

let Server (mailbox: Actor<_>) =
    let mutable hcount=0
    let rec loop() = actor {

        return! loop()
    }
    loop()


let main(args: array<string>) =
    // if args.Length < 1 then
        
    let mutable actor = spawn system "server" 
        0
    // else
    //     //spawn client
    let n,r = int(args.[3]),int(args.[4])
    let mutable errorFlag = false
    numNodes <-n
    numTweets <-r

main(Environment.GetCommandLineArgs())
System.Console.ReadKey() |> ignore
