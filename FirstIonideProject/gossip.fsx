// #time "on"
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.TestKit
open System.Diagnostics
open System.Collections.Generic

let mutable numNodes:int = 0
let mutable topology, algorithm = "",""
let rnd = System.Random()
let mutable workersList = []

let listPrinter (a: List<uint64>) = 
    for i in a do
        printf "%d, " i
    printfn ""

let system = ActorSystem.Create("FSharp")

type BossMessage = 
    | BossMessage of int
    | WorkerTaskFinished of int

type WorkerMessage = WorkerMessage of int * string

// *********** WORKER ACTOR LOGIC **********
let WorkerActor (mailbox: Actor<_>) =
    let mutable hcount=0
    let rec loop() = actor {
        let! WorkerMessage(idx , gossip) = mailbox.Receive()
        let  randState = rnd.Next()%2
        printf "idx: %d, Msg %s Randstate %d\n" idx gossip randState
        
        if hcount = 10 then
            printf "Doneidx: %d, Msg %s" idx gossip
            mailbox.Sender() <! WorkerTaskFinished(1)
        else 
            if idx = 1 then
                workersList.[2] <! WorkerMessage(2,"gossip")
            elif idx = numNodes then
                workersList.[numNodes-1] <! WorkerMessage(numNodes-1,"gossip")
            elif randState = 0 then
                workersList.[idx-1] <! WorkerMessage(idx-1,"gossip")
            else
                workersList.[idx+1] <! WorkerMessage(idx+1,"gossip")
        hcount <- hcount+1
        return! loop()
    }
    loop()


// *************** SUPERVISOR ACTOR'S HELPER UTILITY **************
let supervisorHelper (N:int) = 
    workersList <- List.init numNodes (fun workerId -> spawn system (string workerId) WorkerActor)
    printfn "l.Count: %i, l.Capacity: %i" numNodes  workersList.Length
    let mutable workerIdNum = 0;
    printfn "# of Nodes = %d\nTopology = %s\nAlgorithm = %s" numNodes topology algorithm
    // for i in 1 .. N do
    workersList.[workerIdNum] <! WorkerMessage(1,"gossip")
        // workerIdNum <- workerIdNum + 1
    

// *********** SUPERVISOR ACTOR LOGIC **********
let SupervisorActor (mailbox: Actor<_>) = 
    // count keeps track of all the workers that finish their work and ping back to the supervisor
    let mutable count  = 0 

    // Logic for measuring CPU and real time
    let proc = Process.GetCurrentProcess() 
    let cputimestamp = proc.TotalProcessorTime

    let timer = new Stopwatch()
    timer.Start()
    let stopWatch = Diagnostics.Stopwatch.StartNew()
    // *****************************************

    let rec loop () = actor {
        let! msg = mailbox.Receive ()
        match msg with
        // Process main input
        | BossMessage(N) ->
            // if(N < 1UL) then
            //     printfn "Please provide positive, non-zero integral values for N and K"
            //     printfn "Press any key to exit..."
            // else
                supervisorHelper N
        | WorkerTaskFinished(c) -> 
            count <- count + c

            if count =  numNodes then
                printfn "========\nResults:"
                printfn "================\n"
                timer.Stop()
                let cputime = (proc.TotalProcessorTime-cputimestamp).TotalMilliseconds
                printfn "CPU time = %dms" (uint64 cputime)
                printfn "Real time = %dms" timer.ElapsedMilliseconds
                printfn "CPU to REAL time ratio = %f" (cputime/float timer.ElapsedMilliseconds)
        return! loop ()
    }
    loop ()


let main(args: array<string>) = 
    let N,topo,algo = int(args.[3]),string(args.[4]),string(args.[5])
    numNodes <-N
    topology<-topo
    algorithm<-algo
    let actorRef = spawn system "SupervisorActor" SupervisorActor
    match topology with
        | "full" -> printfn "full"
        | "2D" -> printfn "2D"
        | "line" -> 
            printfn "line"
            actorRef <! BossMessage(numNodes)
        | "imp2D" -> printfn "imp2D"
        | _ -> printfn "Topology not present"
    match algo with
        | "gossip" -> printfn "gossip"
        | "push-sum" -> printfn "push-sum"
        | _ -> printfn "Algorithm not present"

main(Environment.GetCommandLineArgs())
// If not for the below line, the program exits without printing anything.
// Please press any key once the execution is done and CPU times have been printed.
// You might have to scroll and see the printed CPU times. (Because of async processing)
System.Console.ReadKey() |> ignore
