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
    let rec loop() = actor {
        let! msg = mailbox.Receive()
        match msg with
        | Register(username, password) ->
            id <- username
            pwd <- password
            serverActor <! SignUp(id,pwd)
        | Login -> 
            serverActor<! SignIn(id,pwd)
        | Logout -> 
            serverActor<! SignOut(id)
        | GetMentions ->
            serverActor <! FindMentions(id)
        | GetSubscribed ->
            serverActor <! FindSubscribed(id)
        | GetHashtags(searchTerm) ->
            serverActor <! FindHashtags(id, searchTerm)
        | SendTweet(content) ->
            serverActor<! RegisterTweet(id, content) 
        | SendReTweet(content,retweetId) ->
            serverActor<! RegisterReTweet(id, content, retweetId) 
        | FollowUser(followed) ->
            serverActor <! Follow(id,followed)
        | Response(response) -> 
            if not response.Status then
                printfn "Server: %s" response.Msg
        | DataResponse(response) ->
            printfn "=============+%s Query Response+=================" id
            for user, tweet in response.Data do
                printfn "@%s : \" %s \"" user tweet
            printfn "============================================="
        | LiveFeed(tweetCreator, tweetContent) ->
            printfn "LIVEUPDATE for @%s >> %s tweeted \n ~~~~~~~~%s\n~~~~~~~~" id tweetCreator tweetContent
                
        return! loop()
    }
    loop()