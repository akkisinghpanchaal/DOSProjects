module ClientMod

#load @"custom_types.fs"
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit"
#r "nuget: FSharp.Data"

open System
open Akka.FSharp
open CustomTypesMod
open System.Net.WebSockets;
open Akka.Actor
open Akka.FSharp
open System.Text;


// open ServerMod

open FSharp.Data

let twitterHostUrl = "http://localhost:8080"

let loginUrl = twitterHostUrl + "/login"
let logoutUrl = twitterHostUrl + "/logout"
let signUpUrl = twitterHostUrl + "/register"
let socketUrl = twitterHostUrl + "/websocket"

let system = ActorSystem.Create("TwitterClient")

let TwitterApiCall (url: string) (method: string) (body: string) = 
    match method with
    | "GET" ->
        Http.RequestString(url, httpMethod = method)
    | "POST" ->
        Http.RequestString(url, httpMethod = "POST", body = TextRequest body)

type ClientMsgs =
    | Listen of ClientWebSocket

let create() = new ClientWebSocket()

let connect (port: int) (uid: string) (ws: ClientWebSocket) = 
    let tk = Async.DefaultCancellationToken
    Async.AwaitTask(ws.ConnectAsync(Uri(sprintf "ws://127.0.0.1:%d/websocket/%s" port uid), tk))

let close (ws: ClientWebSocket) =
    let tk = Async.DefaultCancellationToken
    Async.AwaitTask(ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close", tk))
    // Async.AwaitTask(ws.SendAsync(ArraySegment(Encoding.UTF8.GetBytes ""), WebSocketMessageType.Close, true, tk))

let send (str: string) (ws: ClientWebSocket) =
    let req = Encoding.UTF8.GetBytes str
    let tk = Async.DefaultCancellationToken
    Async.AwaitTask(ws.SendAsync(ArraySegment(req), WebSocketMessageType.Text, true, tk))

let read (ws: ClientWebSocket) = 
    let loop = true
    async {
        while loop do
            let buf = Array.zeroCreate 4096
            let buffer = ArraySegment(buf)
            let tk = Async.DefaultCancellationToken
            let r =  Async.AwaitTask(ws.ReceiveAsync(buffer, tk)) |> Async.RunSynchronously
            if not r.EndOfMessage then failwith "too lazy to receive more!"
            let resp = Encoding.UTF8.GetString(buf, 0, r.Count)
            if resp <> "" then
                printfn "WebSocket: %s" resp
            else
                printfn "Received empty response from server"
    } |> Async.Start

let SocketListener (mailbox: Actor<_>) =
    let rec loop() = actor {
        let! msg = mailbox.Receive()
        // printfn "    Kuch to aya"
        match msg with
        | Listen(ws) ->
            // printfn "Client: Socket Started Listening"
            read ws
        return! loop()
    }
    loop()

let connectAndPing (ws: ClientWebSocket)(port: int) (uid:string)= async {
    do! connect port uid ws
    System.Threading.Thread.Sleep(100)    
    do! send ("Hello from "+uid) ws
    // close ws |> ignore
    let listener = spawn system ("listener"+uid) SocketListener
    listener <! Listen(ws)
}

let Client (mailbox: Actor<_>) =
    let mutable id,pwd="",""
    let ws = new ClientWebSocket()
    let rec loop() = actor {
        let! msg = mailbox.Receive()
        match msg with
        | Register(username, password) ->
            id <- username
            pwd <- password
            // serverActor <! SignUp(id, pwd)
            let resp = TwitterApiCall signUpUrl "POST" (sprintf """{"Uid":"%s", "Password":"%s"}""" id pwd)
            printfn "Response from server: %s" resp
        | Login -> 
            let resp = TwitterApiCall loginUrl "POST" (sprintf """{"Uid":"%s", "Password":"%s"}""" id pwd)
            printfn "Response from server: %s" resp
            select ("/user/"+id) system <! InitSocket
        | InitSocket -> 
            let resp = TwitterApiCall (socketUrl + "/" + id) "GET" ""
            connectAndPing ws 8080 id |> Async.RunSynchronously
            printfn "Response from server: %s" resp
        | Logout -> 
            let resp = TwitterApiCall logoutUrl "POST" (sprintf """{"Uid":"%s", "Password":"%s"}""" id "")
            printfn "Response from server: %s" resp
            close ws |> Async.RunSynchronously
            // send "CLOSE" ws |> Async.RunSynchronously
            // send ws |> Async.RunSynchronously
        // | GetMentions ->
        //     serverActor <! FindMentions(id)
        // | GetSubscribed ->
        //     serverActor <! FindSubscribed(id)
        // | GetHashtags(searchTerm) ->
        //     serverActor <! FindHashtags(id, searchTerm)
        // | SendTweet(content) ->
        //     serverActor<! RegisterTweet(id, content) 
        // | SendReTweet(content,retweetId) ->
        //     serverActor<! RegisterReTweet(id, content, retweetId) 
        // | FollowUser(followed) ->
        //     serverActor <! Follow(id,followed)
        // | LiveFeed(tweetCreator, tweetContent) ->
        //     printfn "Live Update for @%s => @%s tweeted %s\n" id tweetCreator tweetContent
        // | ApiResponse(response, status) ->
        //     if not status then
        //         printfn "Server: %s" response
        //     select "/user/simulator" system  <! UnitTaskCompleted
        // | ApiDataResponse(response, status, data) ->
        //     printfn "=============+%s Query Response+=================" id
        //     for user, tweet in data do
        //         printfn "@%s : \" %s \"" user tweet
        //     printfn "============================================="
        // | _ ->
        //     printf "Maaaaoooo!!"
        return! loop()
    }
    loop()