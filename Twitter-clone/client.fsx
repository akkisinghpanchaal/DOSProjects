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

// let randomStr (n: int) = 
//     let chars = "abcdefghijklmnopqrstuvwxyz0123456789"
//     let charsLen = chars.Length
//     let random = System.Random()

//     fun len -> 
//         let randomChars = [|for i in 0..len -> chars.[random.Next(charsLen)]|]
//         System.String(randomChars)
let randomStr = 
    // let chars = "ABCDEFGHIJKLMNOPQRSTUVWUXYZ0123456789"
    let chars = "abcdefghijklmnopqrstuvwxyz0123456789"
    let charsLen = chars.Length
    let random = System.Random()

    fun len -> 
        let randomChars = [|for i in 0..len -> chars.[random.Next(charsLen)]|]
        System.String(randomChars)

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
        | Response(res) -> 
            printfn "Response from server: %s" res
        | SendTweet(cntnt) ->
            serverActor<! RegisterTweet(id, cntnt) 
        |_ ->
            printf "Default\n"
        return! loop()
    }
    loop()