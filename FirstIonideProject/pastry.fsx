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
let actorMap:Map<String,IActorRef> = Map.empty
let actorHopsMap:Map<String,Array> = Map.empty
let srcdst:Map<String,String> = Map.empty



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
    | UpdateRoutingTable of Array


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
    let rec loop () = actor {
        let! msg = mailbox.Receive ()
        match msg with
            | Init(passedId) ->
                // Initialization phase of a network node
                id <- passedId
                let number = Convert.ToInt32("15", 16)
                let mutable left,right=number,number
                if left=0 then
                    left <- Map.Count(actorMap) -1
                if right=0 then
                    right <- actorMap.Count -1
                for i in [1..8] do
                    leafSet <- leafSet.Add((left-i).ToString())
                    leafSet <- leafSet.Add((right+i).ToString())
                    
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
    // match algo.ToLower() with
    //     | "gossip" ->
    //         printfn "gossip"
    //     | "push-sum" ->
    //         printfn "push-sum"
    //     | _ ->
    //         errorFlag <- true
    //         printfn "ERROR: Algorithm not present"

    // match topology.ToLower() with
    //     | "full" -> 
    //         printfn "full"
    //     | "2d" -> 
    //         printfn "2D" 
    //         rowSz <- numNodes |> float |> sqrt |> ceil |> int 
    //         numNodes <- (rowSz * rowSz)
    //         printfn "# of Nodes rounded up to:%d" numNodes
    //     | "line" -> 
    //         printfn "line"
    //     | "imp2d" -> 
    //         rowSz <- numNodes |> float |> sqrt |> ceil |> int 
    //         numNodes <- (rowSz * rowSz)
    //         printfn "# of Nodes rounded up to:%d" numNodes
    //         printfn "imp2D"
    //     | _ -> 
    //         errorFlag <- true
    //         printfn "ERROR: Topology '%s' not implemented." topology
    //         printfn "Valid topologies are: full, 2D, imp2D, line."

    // if not errorFlag then
    printfn "bye"

main(Environment.GetCommandLineArgs())
// If not for the below line, the program exits without printing anything.
// Please press any key once the execution is done and CPU times have been printed.
// You might have to scroll and see the printed CPU times. (Because of async processing)
System.Console.ReadKey() |> ignore
