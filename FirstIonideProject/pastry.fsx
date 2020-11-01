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


let main(args: array<string>) = 
    let N,R = int(args.[3]),int(args.[4])
    numDigits <- Math.Log(numNodes|> float,16.00) |> ceil |> int
    let mutable errorFlag = false
    numNodes <-N
    numRequests <-R
    let d = Math.Log(numNodes|> float)
    printf "N:%d\nR:%d\nNumber of digits:%d\n" numNodes numRequests numDigits
    // let actorRef  = spawn system "SupervisorActor" SupervisorActor
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
    //     actorRef <! BossMessage 0

main(Environment.GetCommandLineArgs())
// If not for the below line, the program exits without printing anything.
// Please press any key once the execution is done and CPU times have been printed.
// You might have to scroll and see the printed CPU times. (Because of async processing)
System.Console.ReadKey() |> ignore
