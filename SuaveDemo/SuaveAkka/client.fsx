// module ClientMod
#r "nuget: FSharp.Data"

open System
open System.IO;
open System.Net.WebSockets;
open System.Text;
open System.Threading;
open System.Threading.Tasks;

open FSharp.Data

let create() = new ClientWebSocket()

let connect (port: int) (ws: ClientWebSocket) = 
    let tk = Async.DefaultCancellationToken
    Async.AwaitTask(ws.ConnectAsync(Uri(sprintf "ws://127.0.0.1:%d/websocket" port), tk))

let close (ws: ClientWebSocket) =
    let tk = Async.DefaultCancellationToken
    Async.AwaitTask(ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "END", tk))

let send (str: string) (ws: ClientWebSocket) =
    let req = Encoding.UTF8.GetBytes str
    let tk = Async.DefaultCancellationToken
    Async.AwaitTask(ws.SendAsync(ArraySegment(req), WebSocketMessageType.Text, true, tk))

let read (ws: ClientWebSocket) = 
    async {
        let buf = Array.zeroCreate 4096
        let buffer = ArraySegment(buf)
        let tk = Async.DefaultCancellationToken
        let r =  Async.AwaitTask(ws.ReceiveAsync(buffer, tk)) |> Async.RunSynchronously
        if not r.EndOfMessage then failwith "too lazy to receive more!"
        return Encoding.UTF8.GetString(buf, 0, r.Count)
    }

// let reset = send "{\"Case\":\"Reset\"}"

/// Connects and sends a reset. Returns the DOM.
// let connectAndReset(port: int) = 
//     let ws = new ClientWebSocket()
//     connect port ws |> ignore 
//     System.Threading.Thread.Sleep(100)    
//     send "Hello" ws |> ignore
//     let resp = read ws |> Async.RunSynchronously 
//     printf "%A" resp
//     let resp = read ws |> Async.RunSynchronously 
//     printf "%A" resp
//     // close ws |> ignore

let connectAndReset(port: int) = async {
    let ws = new ClientWebSocket()
    do! connect port ws
    // System.Threading.Thread.Sleep(100)    
    do! send "Hello" ws// |> ignore
    // let resp = read ws |> Async.RunSynchronously 
    // printf "%A" resp
    // let resp = read ws |> Async.RunSynchronously 
    // printf "%A" resp
    // close ws |> ignore
    // return! read ws
    printfn "yesdf g asdoif"
    while true do
        let resp = read ws |> Async.RunSynchronously
        printfn "this is some response: %A" resp
}

// connectAndReset(8080) |> Async.RunSynchronously

type Creds = {
    Uid: string
    Password: string
}

let pl = """{"Uid"="Rajat","Password"="lassan"}"""

// let resp = Request.createUrl Get "http://localhost:8080/" 
//             |> Request.responseAsString
//             |> run

// let resp2 = Request.createUrl Post "http://localhost:8080/login" 
//             |> Request.body (BodyString pl)
//             |> Request.responseAsString
//             |> run


let resp = Http.RequestString( "http://localhost:8080/", httpMethod = "GET")
printfn "%A" resp

let resp2 = Http.RequestString( "http://localhost:8080/login", httpMethod = "POST",
                body = TextRequest """ {"Uid": "rajat", "Password":"lassan"} """)
                // headers = [ "Accept", "application/json" ])
printfn "%A" resp2

if resp2 = "Success Login" then
    connectAndReset(8080) |> Async.RunSynchronously


System.Console.ReadKey() |> ignore