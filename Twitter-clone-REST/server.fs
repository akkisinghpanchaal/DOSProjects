module ServerMod

open System
open Akka.Actor
open Akka.FSharp
open CustomTypesMod
open GlobalDataMod
open UserMod
open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open Suave
open Suave.Operators
open Suave.Filters
open Suave.Successful
open Suave.RequestErrors
open Suave.Logging

open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket



let mutable clientWsDict: Map<string, WebSocket> = Map.empty
let mutable globalData = GlobalData()

let userExists (username:string) = 
    globalData.Users.ContainsKey username

let checkUserLoggedIn (username: string) =
    globalData.IsUserLoggedIn username

let signUpUser (username: string) (password: string) = 
    let mutable response, status = "", false
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
    response, status

let signInUser (username:string) (password: string) =
    let mutable response, status = "", false
    if not (userExists username) then
        response <- sprintf "User %s does not exist in the database." username
    elif (globalData.LoggedInUsers.Contains username) then
        response <- sprintf "User %s is already logged in." username
    else
        globalData.MarkUserLoggedIn username
        response <- sprintf "User %s logged in." username
        status <- true
    response, status


let distributeTweet (username: string) (content: string) (isRetweeted: bool) (parentTweetId: int) =
    let mutable response, status, tweetID = "", false, -1
    if not (userExists username) then
        response <- "Error: User " + username + " does not exist in the database."
    elif not (globalData.LoggedInUsers.Contains username) then
        response <- "Error: User " + username + " is not logged in." 
    else
        if not isRetweeted then
            tweetID <- globalData.AddTweet content username
            response <- "Tweet registered successfully"
        else
            tweetID <- globalData.AddReTweet content username parentTweetId
            response <- "ReTweet registered successfully"
        status<-true
    response, status, tweetID

let signOutUser (username:string) = 
    let mutable response, status = "", false
    if not (globalData.LoggedInUsers.Contains username) then
        response<- "User is either not an valid user of not logged in."
    else
        globalData.MarkUserLoggedOut username
        response<- "User logged out successfully."
        status <- true
    response, status

let followAccount (followerUsername: string) (followedUsername: string) =
    // printfn "followerUserName: %s | followedUserName: %s" followerUsername followedUsername
    let mutable response, status = "", false
    // printfn "Follow stage 1"
    globalData.Users.[followedUsername].AddToFollowers(followerUsername)
    // printfn "Follow stage 2"
    globalData.Users.[followerUsername].AddToFollowings(followedUsername)
    // printfn "Follow stage 3"
    response <- "User " + followerUsername + " started following user " + followedUsername
    status <- true
    response, status

let findHashtags (username: string)(searchTerm: string) =
    let mutable response, status = "", false
    let data  = [|for tweetId in globalData.Hashtags.[searchTerm] do yield (globalData.Tweets.[tweetId].Creator ,globalData.Tweets.[tweetId].Content)|]
    status <- true
    response <- "Successfully retrieved " + string data.Length + " tweets."
    response, status, data

let findTweets (username: string) (searchType: QueryType) =
    let mutable response, status = "", false
    match searchType with
    | QueryType.MyMentions ->
        if (globalData.Users.ContainsKey username) && (globalData.LoggedInUsers.Contains username) then
            status <- true
            let data = [|for tweetId in globalData.Users.[username].MentionedTweets do yield (globalData.Tweets.[tweetId].Creator ,globalData.Tweets.[tweetId].Content)|]
            response <- "Successfully retrieved " + string data.Length + " tweets."
            response, status, data
        elif not (globalData.LoggedInUsers.Contains username) then
            status <- false
            response <- "User " + username + " is not logged in."
            response, status, [||]
        else
            status <- false
            response <-"User " + username + " does not exist in the database."
            response, status, [||]
    | QueryType.Subscribed ->
        if (globalData.Users.ContainsKey username) && (globalData.LoggedInUsers.Contains username) then
            status <- true
            let data = [|for tweetId in globalData.Users.[username].Tweets do yield (globalData.Tweets.[tweetId].Creator ,globalData.Tweets.[tweetId].Content)|]
            response <- "Successfully retrieved " + string data.Length + " tweets."
            response, status, data
        elif not (globalData.LoggedInUsers.Contains username) then
            status <- false
            response <- "User " + username + " is not logged in."
            response, status, [||]
        else
            status <- false
            response <-"User " + username + " does not exist in the database."
            response, status, [||]
// ------------------------------------------------------------------------------
//move to appropriate place
let getBytes (msg:string) =
            msg
            |> System.Text.Encoding.ASCII.GetBytes
            |> ByteSegment

let Server (mailbox: Actor<_>) =
    let mutable loggedInUserToClientMap: Map<string, IActorRef> = Map.empty
    let rec loop() = actor {
        let! msg = mailbox.Receive()
        match msg with
        | SignUp(username,pwd) ->
            mailbox.Sender() <! signUpUser username pwd
        | SignIn(username, pwd) ->
            let response, status = signInUser username pwd
            if status then
                loggedInUserToClientMap <- loggedInUserToClientMap.Add(username, mailbox.Sender())
            mailbox.Sender() <! (response, status)
        | SignOut(username) ->
            let response, status = signOutUser username
            printfn "%s %b User : %s" response status username
            if status then
                loggedInUserToClientMap <- loggedInUserToClientMap.Remove(username)
            mailbox.Sender() <! (response, status)
        | RegisterTweet(senderUser, content) ->
            let response, status, tweetID = distributeTweet senderUser content false -1
            if status then
                let tweet = globalData.Tweets.[tweetID]
                for mentioned in tweet.Mentions do
                    if globalData.IsUserLoggedIn mentioned then
                        loggedInUserToClientMap.[mentioned] <! LiveFeed(tweet.Creator, tweet.Content)
                let followers = globalData.Users.[tweet.Creator].Followers
                for follower in followers do
                    if globalData.IsUserLoggedIn follower then
                        loggedInUserToClientMap.[follower] <! LiveFeed(tweet.Creator, tweet.Content)
            mailbox.Sender() <! (response, status)
        | RegisterReTweet(senderUser, content, subjectTweetId) ->
            let response, status, tweetID = distributeTweet senderUser content true subjectTweetId
            if status then
                let tweet = globalData.Tweets.[tweetID]
                let followers = globalData.Users.[tweet.Creator].Followers
                for follower in followers do
                    if globalData.IsUserLoggedIn follower then
                        loggedInUserToClientMap.[follower] <! LiveFeed(tweet.Creator, tweet.Content)
            mailbox.Sender() <! (response, status)
        | Follow(follower, followed) ->
            let response, status = followAccount follower followed
            mailbox.Sender() <! (response, status)
        | FindHashtags(username, searchTerm) ->
            let response, status, data = findHashtags username searchTerm
            mailbox.Sender() <! (response, status, data)
        | FindSubscribed(username) ->
            let response, status, data = findTweets username Subscribed
            mailbox.Sender() <! (response, status, data)
        | FindMentions(username) ->
            let response, status, data = findTweets username MyMentions
            mailbox.Sender() <! (response, status, data)
        | ShowData ->
            printfn "%A\n%A\n%A\n%A" globalData.Users globalData.LoggedInUsers globalData.Tweets globalData.Hashtags
        | _ -> failwith "Error"
        return! loop()
    }
    loop()

let system = ActorSystem.Create("TwitterServer")
let server = spawn system "server" Server

// -------------------------------REST+SOCKET-----------------------------------
// ------------------------------------------------------------------------------
let JSON v =
    let jsonSerializerSettings = JsonSerializerSettings()
    jsonSerializerSettings.ContractResolver <- CamelCasePropertyNamesContractResolver()

    JsonConvert.SerializeObject(v, jsonSerializerSettings)
    |> OK
    >=> Writers.setMimeType "application/json; charset=utf-8"

let fromJson<'a> json =
  JsonConvert.DeserializeObject(json, typeof<'a>) :?> 'a

let getArgsFromJsonString json =
  JsonConvert.DeserializeObject(json, typeof<PayloadArgs>) :?> PayloadArgs


let getString (rawForm: byte[]) = System.Text.Encoding.UTF8.GetString(rawForm)

let getResourceFromReq<'a> (req : HttpRequest) =
    let getString (rawForm: byte[]) = System.Text.Encoding.UTF8.GetString(rawForm)
    req.rawForm |> getString |> fromJson<'a>

let parseArgs (req : HttpRequest) =
    req.rawForm |> getString |> getArgsFromJsonString


let registerUser (req: HttpRequest) = 
    let creds = parseArgs req
    let task = server <? SignUp(creds.Arg1, creds.Arg2)
    let (resp:string) , (status:bool) = Async.RunSynchronously(task)
    if status then
        OK resp
    else
        NOT_ACCEPTABLE resp

let loginUser (req: HttpRequest) = 
    let creds = parseArgs req
    let task = server <? SignIn(creds.Arg1, creds.Arg2)
    let (resp:string) , (status:bool) = Async.RunSynchronously(task)
    if status then
        OK resp
    else
        NOT_ACCEPTABLE resp

let logoutUser (req: HttpRequest) = 
    let creds = parseArgs req
    let task = server <? SignOut(creds.Arg1)
    let (resp:string) , (status:bool) = Async.RunSynchronously(task)
    
    // clientWsDict.[creds.Uid].send Close (getBytes "END") false |> ignore
    // clientWsDict <- clientWsDict.Remove(creds.Uid)
    if status then
        printfn "\n\n Sending %s" "OK"
        OK resp
    else
        printfn "\n\n Sending %s" "MAA CHUDA LO"
        NOT_ACCEPTABLE resp

let followUser (req: HttpRequest) = 
    let creds = parseArgs req
    // printfn "parsed args are: %A" creds
    let task = server <? Follow(creds.Arg1, creds.Arg2)
    let (resp:string) , (status:bool) = Async.RunSynchronously(task)
    // printfn "Follow res: %s" resp
    OK resp


let postTweet (req: HttpRequest) = 
    let creds = parseArgs req
    let task = server <? RegisterTweet(creds.Arg1, creds.Arg2)
    let (resp:string) , (status:bool) = Async.RunSynchronously(task)
    // printfn "Follow res: %s" resp
    OK resp

let postReTweet (req: HttpRequest) = 
    let creds = parseArgs req
    let task = server <? RegisterReTweet(creds.Arg1, creds.Arg2, int(creds.Arg3))
    let (resp:string) , (status:bool) = Async.RunSynchronously(task)
    // printfn "Follow res: %s" resp
    OK resp


let webSocketFactory (uid: string) = 
  let ws (webSocket : WebSocket) (context: HttpContext) =
    socket {
      // if `loop` is set to false, the server will stop receiving messages
      let mutable loop = true
      clientWsDict <- clientWsDict.Add(uid,webSocket)
      printfn "web socket stored for: %s | total: %d | total logged in Users: %d" uid clientWsDict.Count globalData.LoggedInUsers.Count
      while loop do
        // the server will wait for a message to be received without blocking the thread
        let! msg = webSocket.read()

        match msg with
        // the message has type (Opcode * byte [] * bool)
        //
        // Opcode type:
        //   type Opcode = Continuation | Text | Binary | Reserved | Close | Ping | Pong
        //
        // byte [] contains the actual message
        //
        // the last element is the FIN byte, explained later
        | (Text, data, true) ->
          // the message can be converted to a string
          let str = UTF8.toString data
          printfn "responding to %s -> %s"  uid str
          let response = sprintf "Hi from clientSocket-%s" uid

          // the response needs to be converted to a ByteSegment
          let byteResponse =
            response
            |> System.Text.Encoding.ASCII.GetBytes
            |> ByteSegment

          // the `send` function sends a message back to the client
          do! webSocket.send Text byteResponse true

        | (Close, _, _) ->
          printfn "CLOSING SOCKET SERVER"
          let emptyResponse = [||] |> ByteSegment
          do! webSocket.send Close emptyResponse true

          // after sending a Close message, stop the loop
          loop <- false

        | _ -> ()
      }
  ws

let app = 
    choose [
        // pathScan "/websocket/%s" (fun s -> (webSocketFactory s |> handShake) )
        GET >=> choose [ 
            path "/" >=> OK "index"
            path "/debug" >=> warbler (fun ctx -> OK (sprintf "Total Sockets in map %d" clientWsDict.Count))
            pathScan "/websocket/%s" (fun s -> ((webSocketFactory s |> handShake) >=> (OK ("Socket Success for "+s))))
            ]
        POST >=> choose
            [ 
                path "/hello" >=> OK "Hello POST!"
                path "/register" >=> request registerUser
                path "/login" >=> request loginUser
                path "/logout" >=> request logoutUser
                path "/follow" >=> request followUser
                path "/postTweet" >=> request postTweet
                path "/postReTweet" >=> request postTweet
                 ]
        NOT_FOUND "Found no handlers." ]

[<EntryPoint>]
let main argv =
    printfn "Hello World from F#!"
    startWebServer { defaultConfig with logger = Targets.create Verbose [||] } app
    0 // return an integer exit code