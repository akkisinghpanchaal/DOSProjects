#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit"

open System
open Akka.Actor
open Akka.FSharp

// ASSUMPTIONS
let b: int = 4
let l: int = 2.0**float(b) |> int

let system = ActorSystem.Create("FSharp")
let rnd = System.Random()
let mutable numRequests:int = 0
let mutable numNodes:int = 0
let mutable numDigits:int = 0
let mutable actorMap = Map.empty
// let mutable actorHopsMap = Map.empty
let mutable srcdst:Map<String,String> = Map.empty
// case class Initialize(id:String,digits:Int)
// case class Route(key:String,source:String,hops:Int)
// case class Join(nodeId:String,currentIndex:Int)
// case class UpdateRoutingTable(rt:Array[String])

type BossMessage = 
    | BossMessage of int
    | WorkerTaskFinished of int

type WorkerMessage = 
    | Init of string
    | Join of string * int
    | Route of string * string * int
    // | UpdateRoutingTable of Array


let SupervisorActor (mailbox: Actor<_>) = 
    // count keeps track of all the workers that finish their work and ping back to the supervisor
    // *****************************************
    let stopWatch = System.Diagnostics.Stopwatch.StartNew()
    let rec loop () = actor {
        let! msg = mailbox.Receive ()
        return! loop ()
    }
    loop ()

let NodeActor (mailbox: Actor<_>) = 
    // count keeps track of all the workers that finish their work and ping back to the supervisor
    // *****************************************
    let mutable id: string = ""
    let cols = l
    let mutable leafSet: Set<string> = Set.empty
    let mutable neighborSet: Set<string> = Set.empty
    let mutable routingTable = Array2D.init numDigits l
    let mutable temp = 0
    let rec loop () = actor {
        let! msg = mailbox.Receive ()
        match msg with
            // Initialization phase of a network node
            | Init(passedId) ->
                id <- passedId
                let number = Convert.ToInt32(id, 16)
                let mutable left,right=number,number
                if left=0 then
                    temp <- actorMap.Count
                    left <- temp-1
                if right=0 then
                    temp <- actorMap.Count
                    right <- temp-1
                for i in [1..8] do
                    leafSet <- leafSet.Add((left-i).ToString())
                    leafSet <- leafSet.Add((right+i).ToString())
                printf "Leaf set %A \n" leafSet
            // Updates routing table for a new node
            | Join (nodeId, currentIndex) ->
                let mutable i = 0
                let mutable k =currentIndex
                // keep incrementing counter while same characters are encountered in the hex node IDs
                while nodeId.[i] = id.[i] do
                    i <- i + 1
                let sharedPrefixLength = i
                let mutable routingRow=[||]
                while k<=sharedPrefixLength do
                    Array2D.blit routingTable  k 0 routingRow 0 targetIndex2 16 length2
                    routingRow <- routingTable[k]
                    routingRow.[Convert.ToInt32(id[sharedPrefixLength]16)] = id
                    // actorMap[key] <! UpdateRoutingTable(routingRow)
                    k+=1
                let rtrow = sharedPrefixLength
                let rtcol = Integer.parseInt(key(sharedPrefixLength).toString,16)
                if routingTable(rtrow)(rtcol)==null then
                    routingTable(rtrow)(rtcol) = key
                else
                    actorMap[routingTable[rtrow][rtcol]] <! Join(key,k)
            |_ ->
                printfn "Error!\ns"
        return! loop ()
    }
    loop ()


let main(args: array<string>) = 
    let n,r = int(args.[3]),int(args.[4])
    let mutable errorFlag = false
    numNodes <-n
    numRequests <-r
    numDigits <- Math.Log(numNodes|> double, 16.) |> ceil |> int
    printf "N:%d\nR:%d\nNumber of digits:%d\n" numNodes numRequests numDigits
    printf "Network construction initiated"
    let actorRef  = spawn system "SupervisorActor" SupervisorActor
    let mutable nodeId:string=String.replicate numDigits "0"
    let mutable hexNum:string=""
    let mutable len = 0
    printf "Node Id: %s\n" nodeId
    let mutable actor = spawn system nodeId NodeActor
    actor <! Init nodeId
    actorMap <- actorMap.Add(nodeId,actor)
    for i in 1 .. numNodes-1 do
        printf "\nNode creating %s\n" nodeId
        // if i = 4 then
        //     printf "25% of network constructed"
        // elif i= numNodes/2 then
        //     printf "50% of network constructed"
        // elif i=numNodes*3/4 then
        //     printf "75% of network constructed"
        hexNum <- i.ToString("X")
        len <- hexNum.Length
        printf "%s numDigits-len %d\n" hexNum (numDigits-len)
        nodeId <- String.concat  "" [String.replicate (numDigits-len) "0"; hexNum]
        actor <- spawn system (string nodeId) NodeActor
        actor <! Init(nodeId)
        actorMap <- actorMap.Add(nodeId,actor)
        actorMap.[String.replicate numDigits "0"] <! Join(nodeId,0)
        System.Threading.Thread.Sleep(3000)
    if not errorFlag then
        actorRef <! BossMessage 0

main(Environment.GetCommandLineArgs())
// If not for the below line, the program exits without printing anything.
// Please press any key once the execution is done and CPU times have been printed.
// You might have to scroll and see the printed CPU times. (Because of async processing)
System.Console.ReadKey() |> ignore
