module ClientMod

#load @"messages.fs"
#load @"server.fsx"

#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 

open ApiMsgs
open Akka.Actor
open Akka.FSharp
open System
open ApiMsgs
open ServerMod

let system = ActorSystem.Create("Twitter")
let mutable serverActor = spawn system "server" Server

let Client (mailbox: Actor<_>) =
    let mutable id,pwd ="",""
    // let mutable srcdst:Map<String,String> = Map.empty
    let rec loop() = actor {
        let! msg = mailbox.Receive()
        match msg with
        | Register(username, password) ->
            id <- username
            pwd <- password
            serverActor <! SignUp(id,pwd)
        | Login -> 
            serverActor<! SignIn(id,pwd)
        | Response(res,status) -> 
            printfn "Client: %s %b" res status
        | SendTweet(cntnt) ->
            serverActor<! RegisterTweet(id, cntnt) 
        |_ ->
            printf "Default\n"
        return! loop()
    }
    loop()