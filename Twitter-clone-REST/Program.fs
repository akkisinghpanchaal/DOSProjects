
// #r "nuget: Akka.Fsharp"
// #r "nuget: Akka.TestKit" 
module AppMod

open System
open Suave
open Suave.Http
open Suave.Operators
open Suave.Filters
open Suave.Successful
open Suave.Files
open Suave.RequestErrors
open Suave.Logging
open Suave.Utils

open System
open System.Net

open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket
open Newtonsoft.Json
open Newtonsoft.Json.Serialization

open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.TestKit

open ServerMod
open CustomTypesMod

let mutable myws:WebSocket array = Array.empty
let system = ActorSystem.Create("TwitterCloneREST")
let server = spawn system "server" Server

let ws (webSocket : WebSocket) (context: HttpContext) =
  socket {
    // if `loop` is set to false, the server will stop receiving messages
    let mutable loop = true
    myws <- [|webSocket|]
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
        printfn "printing response"
        printfn "response to %s" str
        let response = sprintf "response to %s" str

        // the response needs to be converted to a ByteSegment
        let byteResponse =
          response
          |> System.Text.Encoding.ASCII.GetBytes
          |> ByteSegment

        do! webSocket.send Text byteResponse true

        // the `send` function sends a message back to the client
        // let mutable inp = ""
        // while inp <> "exit" do
        //   inp <- System.Console.ReadLine()
        //   do! webSocket.send Text byteResponse true
        // System.Threading.Thread.Sleep(10)    
        // do! webSocket.send Text byteResponse true
        // System.Threading.Thread.Sleep(10)    
        // do! webSocket.send Text byteResponse true
        // System.Threading.Thread.Sleep(10)    
        // do! webSocket.send Text byteResponse true
      | (Close, _, _) ->
        let emptyResponse = [||] |> ByteSegment
        do! webSocket.send Close emptyResponse true

        // after sending a Close message, stop the loop
        loop <- false

      | _ -> ()
    }

/// An example of explictly fetching websocket errors and handling them in your codebase.
let wsWithErrorHandling (webSocket : WebSocket) (context: HttpContext) = 
   
   let exampleDisposableResource = { new IDisposable with member __.Dispose() = printfn "Resource needed by websocket connection disposed" }
   let websocketWorkflow = ws webSocket context
   
   async {
    let! successOrError = websocketWorkflow
    match successOrError with
    // Success case
    | Choice1Of2() -> ()
    // Error case
    | Choice2Of2(error) ->
        // Example error handling logic here
        printfn "Error: [%A]" error
        exampleDisposableResource.Dispose()
        
    return successOrError
   }
type Credentials = {
    Uid: string
    Password: string
}

let JSON v =
    let jsonSerializerSettings = JsonSerializerSettings()
    jsonSerializerSettings.ContractResolver <- CamelCasePropertyNamesContractResolver()

    JsonConvert.SerializeObject(v, jsonSerializerSettings)
    |> OK
    >=> Writers.setMimeType "application/json; charset=utf-8"

let fromJson<'a> json =
  JsonConvert.DeserializeObject(json, typeof<'a>) :?> 'a

let getCredsFromJsonString json =
  JsonConvert.DeserializeObject(json, typeof<Credentials>) :?> Credentials


let getResourceFromReq<'a> (req : HttpRequest) =
    let getString (rawForm: byte[]) = System.Text.Encoding.UTF8.GetString(rawForm)
    req.rawForm |> getString |> fromJson<'a>

let parseCreds (req : HttpRequest) =
    let getString (rawForm: byte[]) = System.Text.Encoding.UTF8.GetString(rawForm)
    req.rawForm |> getString |> getCredsFromJsonString


let loginUser (req: HttpRequest) = 
    let creds = parseCreds req
    let task = server <? SignIn(creds.Uid, creds.Password)
    let resp = Async.RunSynchronously(task)
    let wresp = (resp:string)
                  |> System.Text.Encoding.ASCII.GetBytes
                  |> ByteSegment
    printfn "Sending a websocket message..."
    // myws.[0].send Text wresp true |> ignore
    OK resp

let registerUser (req: HttpRequest) = 
    let creds = parseCreds req
    let task = server <? SignUp(creds.Uid, creds.Password)
    let resp = Async.RunSynchronously(task)
    OK resp


let app = 
    choose [
        path "/websocket" >=> handShake ws
        path "/websocketWithSubprotocol" >=> handShakeWithSubprotocol (chooseSubprotocol "test") ws
        path "/websocketWithError" >=> handShake wsWithErrorHandling
        GET >=> choose [ path "/" >=> OK "index" ]
        POST >=> choose
            [ 
                path "/hello" >=> OK "Hello POST!"
                path "/register" >=> request registerUser
                path "/login" >=> request loginUser
                 ]
        NOT_FOUND "Found no handlers." ]


[<EntryPoint>]
let main argv =
    printfn "Hello World from F#!"
    startWebServer { defaultConfig with logger = Targets.create Verbose [||] } app
    0 // return an integer exit code