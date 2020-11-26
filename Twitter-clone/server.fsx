module ServerMod

#load @"messages.fs"
#load @"user.fs"
#load @"global_data.fsx"

#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 

open System

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

let distributeTweet (creatorUsername: string) (tweetContent: string) =
    printf "Todo"

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
    let mutable srcdst:Map<String,String> = Map.empty
    let rec loop() = actor {
        let! msg = mailbox.Receive()
        match msg with
        | SignUp(username,pwd) ->
            if signUpUser username pwd then
                printfn "Successfully Signed up User: %s\n" username
            else
                printfn "Could not signup user %s." username
        | SignIn(username, pwd) ->
            if signInUser username pwd then
                printfn "Successfully Signed In User: %s\n" username
            else
                printfn "Could not signIn user %s." username
        | SignOut(username) ->
            if signOutUser username then
                printfn "Successfully Signed out User: %s\n" username
            else
                printfn "Could not sign out user %s." username
        | SendTweet(senderUser, content) ->
            printfn "User: %s and Tweet: %s" senderUser content
        | Follow(follower, followed) ->
            printfn "User %s is now following user %s" follower followed
        | SendReTweet(username, subjectTweetId) ->
            printfn "User: %s and ReTweeted: %s" username subjectTweetId
        | FindTweets(a) ->
            printf "sdf"
        | ShowData ->
            printfn "%A\n%A" globalData.Users globalData.LoggedInUsers
        // | _ ->
        //     printfn "server test"
        return! loop()
    }
    loop()