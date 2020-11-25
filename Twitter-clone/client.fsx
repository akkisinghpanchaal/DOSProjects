#load @"messages.fs"
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 
open ApiMsgs
open Akka.Actor
open Akka.FSharp
open System
let system = ActorSystem.Create("Twitter")

let Client (mailbox: Actor<_>) =
    let mutable srcdst:Map<String,String> = Map.empty
    let engine  = select "/user/server" system
    let rec loop() = actor {
        let! msg = mailbox.Receive()
        match msg with
        | Init(username) ->
            printfn" Aa gaya INIT customer %s\n" username 
            engine <! SignUp("loda","lassun")
        |_ ->
            printfn "server test"
        return! loop()
    }
    loop()