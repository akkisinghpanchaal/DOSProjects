// Learn more about F# at http://fsharp.org

open System


open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Newtonsoft.Json
open Newtonsoft.Json.Serialization

type Londa = {
    Name: string
    Age: int
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
    if creds.Password = "lassan" then
        OK "You're good!"
    else
        OK "maa chuda!"

// let rest resourceName (resource: 'a) =
//   let resourcePath = "/" + resourceName
//   let gellAll = resource.GetAll () |> JSON
//   path resourcePath >=> GET >=> getAll


let londa = {
    Name="Akshay Kumar"
    Age=25
}

let dusralonda = {
    Name="Akshay Kumar"
    Age=25
}

let londe = [|londa; dusralonda|]

let app =
    choose
        [ GET >=> choose
            [ path "/" >=> OK "Index"
              path "/hello" >=> OK "Hello!"
              path "/londabhejo" >=> JSON londa
              path "/londebhejo" >=> JSON londe
              pathScan "/hello/%s" (fun (a) -> OK (sprintf "You passed: %s" ((a).ToString()))) ]
          POST >=> choose
            [ 
                path "/hello" >=> OK "Hello POST!"
                path "/login" >=> request loginUser
                 ] ]

[<EntryPoint>]
let main argv =
    printfn "Hello World from F#!"
    startWebServer defaultConfig app            
    0 // return an integer exit code
