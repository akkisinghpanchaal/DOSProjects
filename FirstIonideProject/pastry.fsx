#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 

open System
open Akka.Actor
open Akka.FSharp

let system = ActorSystem.Create("FSharp")
let rnd = System.Random()
let mutable numRequests:int = 0
let mutable numNodes:int = 0
let mutable numDigits:int = 0
//   var actorMap:HashMap[String,ActorRef] = HashMap()
//   var actorHopsMap:HashMap[String,Array[Double]] = HashMap()
//   var srcdst:HashMap[String,String] = HashMap()



// case class Initialize(id:String,digits:Int)
// case class Route(key:String,source:String,hops:Int)
// case class Join(nodeId:String,currentIndex:Int)
// case class UpdateRoutingTable(rt:Array[String])

type BossMessage = 
    | BossMessage of int
    | WorkerTaskFinished of int

type WorkerMessage = 
    | Init of string * string
    | WorkerMessagePushSum of int * float * float


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
    let stopWatch = System.Diagnostics.Stopwatch.StartNew()
    let rec loop () = actor {
        let! msg = mailbox.Receive ()
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

    if not errorFlag then
        0

main(Environment.GetCommandLineArgs())
// If not for the below line, the program exits without printing anything.
// Please press any key once the execution is done and CPU times have been printed.
// You might have to scroll and see the printed CPU times. (Because of async processing)
System.Console.ReadKey() |> ignore
