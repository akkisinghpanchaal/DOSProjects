module ClientMod

#load @"custom_types.fsx"
#load @"server.fsx"
#load @"tweet.fs"

#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 

open CustomTypesMod
open Akka.Actor
open Akka.FSharp
open System
open ServerMod
open TweetMod

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
        | SendTweet(content) ->
            serverActor<! RegisterTweet(id, content) 
        | SendReTweet(content,retweetId) ->
            serverActor<! RegisterReTweet(id, content, retweetId) 
        | FollowUser(followed) ->
            serverActor <! Follow(id,followed)
        | GetTweets(username, searchTerm, queryType) ->
            serverActor <! FindTweets(username, searchTerm, queryType)
        | Response(response) -> 
            printfn "Server: %s %b" response.Response response.Status
            printfn "%A" response.Data
        | DataResponse(response) ->
            for row in response.Data do
                printfn "Tweet Id: %d\nContent: %s\nParent: %d\nCreator: %s" row.Id row.Content row.ParentTweetId row.Creator
            // if response.Data.Length > 0 then
                // response.Data |>  List.iter (fun row -> printfn "Tweet Id: %d\nContent: %s\nParent: %d\nCreator: %s" row.Id row.Content row.ParentTweetId row.Creator)
        return! loop()
    }
    loop()