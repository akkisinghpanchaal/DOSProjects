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
    let mutable signUpStatus = false
    if userExists username then
        printfn "User '%s' already exists in the database." username
    elif username.Contains " " then
        printfn "Invalid characters found in username. Use only alphanumeric characters."
    else
        // All ok. Create user now.
        let mutable newUserObj = User(username, password)
        globalData.AddUsers username newUserObj
        printfn "Added user %s to the database." username
        signUpStatus <- true
    signUpStatus

let signInUser (username:string) (password: string) =
    let mutable signInStatus = false
    if not (userExists username) then
        printfn "User %s does not exist in the database." username
    elif (globalData.LoggedInUsers.Contains username) then
        printfn "User %s is already logged in." username
    else
        globalData.MarkUserLoggedIn username
        signInStatus <- true
    signInStatus

let distributeTweet (username: string) (cntnt: string) =
    let mutable response,status = "",false
    if not (userExists username) then
        response <- "Error: User " + username + " does not exist in the database."
    elif not (globalData.LoggedInUsers.Contains username) then
        response <- "Error: User " + username + " is not logged in." 
    else
        let tweet = Tweet(tweetAutoIncrement,username,cntnt)
        globalData.AddTweets tweetAutoIncrement tweet
        response <- "Success"
        status<-true
    response,status

let signOutUser (username:string): bool = 
    let mutable signOutStatus = false
    if not (globalData.LoggedInUsers.Contains username) then
        printfn "User is either not an valid user of not logged in."
    else
        globalData.MarkUserLoggedOut username
        signOutStatus <- true
    signOutStatus

// ------------------------------------------------------------------------------

let Server (mailbox: Actor<_>) =
    let mutable response:string = ""
    let rec loop() = actor {
        let! msg = mailbox.Receive()
        match msg with
        | SignUp(username,pwd) ->
            if signUpUser username pwd then
                response <- "Successfully Signed up User:" + username
            else
                response <- "Could not signup user" + username
        | SignIn(username, pwd) ->
            if signInUser username pwd then
                response <- "Successfully Signed In User: " + username
            else
                response <- "Could not signIn user." + username
            mailbox.Sender() <! Response(response)
        | SignOut(username) ->
            if signOutUser username then
                response <- "Successfully Signed out User:" + username
            else
                response <- "Could not sign out user: " + username
            mailbox.Sender() <! Response(response)
        | RegisterTweet(senderUser, content) ->
            printfn "User: %s and Tweet: %s" senderUser content
            let res,status = distributeTweet senderUser content
            mailbox.Sender() <! Response(res)
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