#load @"messages.fs"
#load @"driver.fsx"
#load @"server.fsx"


#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 

open ApiMsgs
open Akka.Actor
open Akka.FSharp
open System
open ServerMod

let system = ActorSystem.Create("Twitter")
let mutable serverActor = spawn system "server" Server

let Client (mailbox: Actor<_>) =
    let mutable srcdst:Map<String,String> = Map.empty
    let rec loop() = actor {
        let! msg = mailbox.Receive()
        match msg with
        | Init(username) ->
            printfn" Aa gaya INIT customer %s\n" username
            serverActor <! SignUp("rajat.rai","rajat@1234")
            serverActor <! SignIn("rajat.rai","rajat@1234")
            serverActor <! SignUp("akshay kumar","rajat@1234")
            serverActor <! SignIn("akshay.kumar","rajat@1234")
            serverActor <! ShowData
            serverActor <! SignOut("rajat.rai")
            serverActor <! ShowData

        // |_ ->
        //     printfn "server test"
        return! loop()
    }
    loop()
    
let main(args: array<string>) =
    let mutable clientActor = spawn system "client" Client
    clientActor <! Init("akkisingh")


main(Environment.GetCommandLineArgs())
System.Console.ReadKey() |> ignore