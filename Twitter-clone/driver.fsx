module Lolo

#load @"user.fs"
#load @"tweet.fs"
#load @"messages.fs"
#load @"server.fsx"

#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 

// open GlobalDataMod
// open TweetMod
open UserMod
open ApiMsgs
open System
open Akka.Actor
open Akka.FSharp


let main(args: array<string>) =
        
    // let mutable actor = spawn system "server" Server
    
    // let n,r = int(args.[3]),int(args.[4])
    // let mutable errorFlag = false
    // numNodes <-n
    // numTweets <-r
    0

// main(Environment.GetCommandLineArgs())
// System.Console.ReadKey() |> ignore
