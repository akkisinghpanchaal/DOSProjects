#load @"messages.fs"
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 
open ApiMsgs
open Akka.Actor
open Akka.FSharp
open System

let Server (mailbox: Actor<_>) =
    let mutable srcdst:Map<String,String> = Map.empty
    let rec loop() = actor {
        let! msg = mailbox.Receive()
        match msg with
        | SignUp(username,pwd) ->
            printfn" Aa gaya customer %s %s\n" username pwd
        |_ ->
            printfn "server test"
        return! loop()
    }
    loop()