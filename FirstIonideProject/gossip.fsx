// #time "on"
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 

open System
open Akka.Actor
open Akka.FSharp

let system = ActorSystem.Create("FSharp")
let rnd = System.Random()

let mutable numNodes:int = 0
let mutable rowSz:int = 0 
let mutable topology, algorithm = "",""
let mutable workersList = [||]
let mutable actorStates= [||] 
type BossMessage = 
    | BossMessage of int
    | WorkerTaskFinished of int

type WorkerMessage = WorkerMessage of int * string

let getRandomNeighbor (idx:int) =
    let mutable neighs = [||]
    match topology with
        | "full" -> 
            let mutable randState = rnd.Next()% numNodes
            while randState = idx do
                randState <- rnd.Next()% numNodes
            neighs <- [|randState|]
            printfn "full"
        | "2D" -> 
            let r = int idx / rowSz
            let c = idx-(rowSz*r)
            if (r+1)< rowSz then
                neighs <- Array.append neighs [|((r+1)*rowSz)+c|]
            if r-1>= 0 then
                neighs <- Array.append neighs [|((r-1)*rowSz)+c|]
            if c+1< rowSz then
                neighs <- Array.append neighs [|(r*rowSz)+c+1|]
            if c-1< rowSz then
                neighs <- Array.append neighs [|(r*rowSz)+c-1|]
            printfn "2D"
        | "line" -> 
            printfn "line"
        | "imp2D" -> 
            printfn "imp2D"
        |_ ->
            printfn "DEf"
    neighs


// *********** WORKER ACTOR LOGIC **********
let GossipActor (mailbox: Actor<_>) =
    let mutable hcount=0
    let rec loop() = actor {
        let! WorkerMessage(idx , gossip) = mailbox.Receive()
        printf "idx: %d heardCount %d minheard %d\n" idx hcount (actorStates |> Array.min)
        actorStates.[idx] <- actorStates.[idx] + 1
        let randState = rnd.Next()%2
        hcount <- hcount+1
        if (actorStates |> Array.min) = 3 then
            mailbox.Sender() <! WorkerTaskFinished(1)
            printf "Done  Msg %s\n" gossip
            printfn "%A" actorStates
        elif numNodes >1 then
            if idx = 0 then
                workersList.[1] <! WorkerMessage(1,"gossip")
            elif idx = numNodes-1 then
                workersList.[numNodes-2] <! WorkerMessage(numNodes-2,"gossip")
            elif randState = 0 then
                workersList.[idx-1] <! WorkerMessage(idx-1,"gossip")
            else
                workersList.[idx+1] <! WorkerMessage(idx+1,"gossip")
        return! loop()
    }
    loop()


// *************** SUPERVISOR ACTOR'S HELPER UTILITY **************
let supervisorHelper (start:int)= 
    workersList <- [| for i in 1 .. numNodes -> spawn system (string i) GossipActor |]
    actorStates <-  Array.zeroCreate numNodes
    printfn "Capacity: %i" workersList.Length
    printfn "# of Nodes = %d\nTopology = %s\nAlgorithm = %s" numNodes topology algorithm
    workersList.[start] <! WorkerMessage(start,"gossip")
    

// *********** SUPERVISOR ACTOR LOGIC **********
let SupervisorActor (mailbox: Actor<_>) = 
    // count keeps track of all the workers that finish their work and ping back to the supervisor
    // *****************************************
    let stopWatch = System.Diagnostics.Stopwatch.StartNew()

    let rec loop () = actor {
        let! msg = mailbox.Receive ()
        match msg with
        // Process main input
        | BossMessage(start) ->
            supervisorHelper start
        | WorkerTaskFinished(c) -> 
            printfn "%A" actorStates
            printfn "========\nResults:"
            printfn "================\n"
            stopWatch.Stop()
            printfn "Totel run time = %fms" stopWatch.Elapsed.TotalMilliseconds
        return! loop ()
    }
    loop ()


let main(args: array<string>) = 
    let N,topo,algo = int(args.[3]),string(args.[4]),string(args.[5])
    let mutable errorFlag = false
    numNodes <-N
    topology<-topo
    algorithm<-algo
    let actorRef  = spawn system "SupervisorActor" SupervisorActor
    match algo with
        | "gossip" ->
            printfn "gossip"
        | "push-sum" ->
            printfn "push-sum"
        | _ ->
            errorFlag <- true
            printfn "ERROR: Algorithm not present"
    match topology with
        | "full" -> 
            printfn "full"
        | "2D" -> 
            printfn "2D" 
            rowSz <- numNodes |> float |> sqrt |> ceil |> int 
            numNodes <- (rowSz * rowSz)
            printfn "# of Nodes rounded up to:%d" numNodes
        | "line" -> 
            printfn "line"
        | "imp2D" -> 
            printfn "imp2D"
        | _ -> 
            errorFlag <- true
            printfn "ERROR: Topology not present"
    if not errorFlag then
        actorRef <! BossMessage 0

main(Environment.GetCommandLineArgs())
// If not for the below line, the program exits without printing anything.
// Please press any key once the execution is done and CPU times have been printed.
// You might have to scroll and see the printed CPU times. (Because of async processing)
System.Console.ReadKey() |> ignore
system.Terminate()
