module ServerMod

#load @"messages.fs"
#load @"user.fs"
#load @"tweet.fs"
#load @"global_data.fsx"

#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 

open System
open TweetMod
open Akka.Actor
open Akka.FSharp

open ApiMsgs
open GlobalDataMod
open UserMod

let mutable globalData = GlobalData()
let mutable userAutoIncrement = 1
let mutable tweetAutoIncrement = 1

// let DataHandler (mailbox: Actor<_>) =
//     let mutable srcdst:Map<String,String> = Map.empty
//     let rec loop() = actor {
//         let! msg = mailbox.Receive()
//         match msg with
//         | SignUp(username,pwd) ->
//             printfn" Aa gaya customer %s %s\n" username pwd
//         | SignIn(username, pwd) ->
//             printfn "Logged in %s %s" username pwd
//         | Tweet(username, content) ->
//             printfn "User: %s and Tweet: %s" username content
//         | Follow(follower, followed)
//             printfn "User %s is now following user %s" follower followed
//         | Retweet(username, subjectTweetId)
//             printfn "User: %s and ReTweeted: %s" username subjectTweetId
//         | FindTweets()
//         |_ ->
//             printfn "server test"
//         return! loop()
    // }
    // loop()

// ==================== Helper Methods for Server ===============================

// let generateNewUserId() = 
//     userAutoIncrement <- userAutoIncrement + 1
//     userAutoIncrement

let userExists (username:string) = 
    globalData.Users.ContainsKey username

let signUpUser (username: string) (password: string) = 
    let mutable response,status = "",false
    if userExists username then
        response <- sprintf "User %s already exists in the database." username
    elif username.Contains " " then
        response <- "Invalid characters found in username. Use only alphanumeric characters."
    else
        // All ok. Create user now.
        let newUserObj = User(username, password)
        globalData.AddUsers username newUserObj
        response <- sprintf "Added user %s to the database." username
        status <- true
    response,status

let signInUser (username:string) (password: string) =
    let mutable response,status = "",false
    if not (userExists username) then
        response <- sprintf "User %s does not exist in the database." username
    elif (globalData.LoggedInUsers.Contains username) then
        response <- sprintf "User %s is already logged in." username
    else
        globalData.MarkUserLoggedIn username
        status <- true
    response,status

let distributeTweet (username: string) (cntnt: string) =
    let mutable response,status = "",false
    if not (userExists username) then
        response <- "Error: User " + username + " does not exist in the database."
    elif not (globalData.LoggedInUsers.Contains username) then
        response <- "Error: User " + username + " is not logged in." 
    else
        let tweet = Tweet(tweetAutoIncrement,username,cntnt)
        globalData.AddTweets tweetAutoIncrement tweet
        response <- "Tweet registered successfully"
        status<-true
    response,status

let signOutUser (username:string) = 
    let mutable response,status = "",false
    if not (globalData.LoggedInUsers.Contains username) then
        response<- "User is either not an valid user of not logged in."
    else
        globalData.MarkUserLoggedOut username
        response<- "User logged out successfully."
        status <- true
    response,status

// ------------------------------------------------------------------------------

let Server (mailbox: Actor<_>) =
    let rec loop() = actor {
        let! msg = mailbox.Receive()
        match msg with
        | SignUp(username,pwd) ->
            let res,status = signUpUser username pwd
            mailbox.Sender() <! Response(res,status)
        | SignIn(username, pwd) ->
            let res,status = signInUser username pwd
            mailbox.Sender() <! Response(res,status)
        | SignOut(username) ->
            let res,status = signOutUser username
            mailbox.Sender() <! Response(res,status)
        | RegisterTweet(senderUser, content) ->
            let res,status = distributeTweet senderUser content
            mailbox.Sender() <! Response(res,status)
        | Follow(follower, followed) ->
            printfn "User %s is now following user %s" follower followed
        | SendReTweet(username, subjectTweetId) ->
            printfn "User: %s and ReTweeted: %s" username subjectTweetId
        | FindTweets(a) ->
            printf "sdf"
        | ShowData ->
            printfn "%A\n%A\n%A" globalData.Users globalData.LoggedInUsers globalData.Tweets
        // | _ ->
        //     printfn "server test"
        return! loop()
    }
    loop()