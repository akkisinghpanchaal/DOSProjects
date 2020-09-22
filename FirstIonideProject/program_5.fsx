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


let totalWorkers = 27
let mutable N_copy:uint64 = 0UL
let firstNSquare (n: uint64) : uint64 = ((n*(n+1UL)*(2UL*n+1UL))/6UL)

let sumSquare (a:uint64) (b:uint64) : uint64 = 
    let res1 = firstNSquare b 
    let res2 = firstNSquare (a-1UL)
    res1 - res2

let isSumPerfectSquare (a:uint64) (b:uint64) : bool = 
    let sqt = sumSquare a b |> float |> sqrt
    sqt = floor sqt

let totalRunningWorkers (N:uint64) = 
    let pws = uint64(ceil(float N / float totalWorkers))
    let mutable c = 0
    for i in 1UL..pws..N do
        c <- c + 1
    c

let system = ActorSystem.Create("FSharp")

type BossMessage = 
    | BossMessage of uint64 * uint64
    | WorkerTaskFinished of uint64 * int

type WorkerMessage = WorkerMessage of uint64 * uint64 * uint64

let WorkerActor (mailbox: Actor<_>) =
    let rec loop() = actor {
        let! WorkerMessage(st, en, k) = mailbox.Receive()
        for firstNum = st to en do
            if isSumPerfectSquare firstNum (firstNum+k-1UL) then
                mailbox.Sender() <! WorkerTaskFinished(firstNum, 1)
        mailbox.Sender() <! WorkerTaskFinished(0UL, 1)
        return! loop()
    }
    loop()


let supervisorHelper (N:uint64) (k: uint64) = 
        
    let perWorkerSegment = uint64(ceil(float N / float totalWorkers))
    printfn "Subproblems per worker = %d" perWorkerSegment

    let workersList = List.init totalWorkers (fun workerId -> spawn system (string workerId) WorkerActor)
    let mutable workerIdNum = 0;
    
    for i in 1UL .. perWorkerSegment .. N do
        workersList.Item(workerIdNum) <! WorkerMessage(i, min N (i+perWorkerSegment-1UL), k)
        workerIdNum <- workerIdNum + 1

let SupervisorActor (mailbox: Actor<_>) = 
    let mutable count  = 0
    let proc = Process.GetCurrentProcess()
    let cpu_time_stamp = proc.TotalProcessorTime
    let timer = new Stopwatch()
    timer.Start()
    let stopWatch = Diagnostics.Stopwatch.StartNew()
    let rec loop () = actor {
        let! msg = mailbox.Receive ()
        match msg with
        | BossMessage(N, k) ->
            if(k < 1UL || N < 1UL) then
                printfn "Please provide positive, non-zero integral values for N and K"
                printfn "Press any key to exit..."
            else
                supervisorHelper N k
        | WorkerTaskFinished(st,c) -> 
            if c=1 && st > 0UL then
                printfn "%d" st
            count <- count + c
            if count = (totalRunningWorkers N_copy) then
                let cpu_time = (proc.TotalProcessorTime-cpu_time_stamp).TotalMilliseconds
                printfn "CPU time = %dms" (uint64 cpu_time)
                printfn "Real time = %dms" timer.ElapsedMilliseconds
                printfn "CPU to REAL time ratio = %f" (cpu_time/float timer.ElapsedMilliseconds)
        return! loop ()
    }
    loop ()


let main(args: array<string>) = 
    let N = uint64(args.[3])
    let k = uint64(args.[4])
    N_copy <- N
    let actorRef = spawn system "SupervisorActor" SupervisorActor
    printfn "Total workers = %d" (totalRunningWorkers N_copy)
    actorRef <! BossMessage(N, k)

main(Environment.GetCommandLineArgs())
System.Console.ReadKey() |> ignore
