// ActorSayHello.fsx
// #time "on"
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 
// #load "Bootstrap.fsx"

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.TestKit
open System.Collections.Generic

let mutable finishedCount = 0
let totalWorkers = 100
let resList = new List<int64>()
let mutable N_copy:int64 = 0L
let mutable finishedList = Array.create 600 false

let firstNSquare (n: int64) : int64 = ((n*(n+1L)*(2L*n+1L))/6L)

let sumSquare (a:int64) (b:int64) : int64 = 
    let res1 = firstNSquare b 
    let res2 = firstNSquare (a-1L)
    res1 - res2

let isSumPerfectSquare (a:int64) (b:int64) : bool = 
    // printfn "%d %d" a b
    let sqt = sumSquare a b |> float |> sqrt
    // if sqt = floor sqt then
    //     printfn "%d, " a
    sqt = floor sqt

let totalRunningWorkers (N:int64) = 
    // printfn "N_copy = %d" N
    let pws = int64(ceil(float N / float totalWorkers))
    let mutable c = 0
    for i in 1L..pws..N do
        c <- c + 1
    c
// #Using Actor
// Actors are one of Akka's concurrent models.
// An Actor is a like a thread instance with a mailbox. 
// It can be created with system.ActorOf: use receive to get a message, and <! to send a message.
// This example is an EchoServer which can receive messages then print them.
let system = ActorSystem.Create("FSharp")

type BossMessage = 
    | BossMessage of int64 * int64
    | WorkerTaskFinished of int64 * int64

type WorkerMessage = WorkerMessage of int64 * int64 * int64

let listPrinter (a: List<int64>) = 
    for i in a do
        printf "%d, " i
    printfn ""

let reduceList =
    finishedList |> Array.contains false

let WorkerActor (mailbox: Actor<_>) =
    let rec loop() = actor {
        let! WorkerMessage(st, en, k) = mailbox.Receive()
        for firstNum = st to en do
            // printfn "At : %d\n" st
            // printfn ""
            if isSumPerfectSquare firstNum (firstNum+k-1L) then
                mailbox.Sender() <! WorkerTaskFinished(firstNum, firstNum+k-1L)
                // resList.Add(st)
            
            // if finishedList.[int(st)-1] = false then
            //     finishedList.[int(st)-1] <- true
            // if not reduceList then
            //     listPrinter resList
        return! loop()
    }
    loop()


let supervisorHelper (N:int64) (k: int64) = 
        
    let perWorkerSegment = int64(ceil(float N / float totalWorkers))
    
    printfn "perWorkerSegment = %d" perWorkerSegment
    
    let workersList = List.init totalWorkers (fun workerId -> spawn system (string workerId) WorkerActor)
    let mutable workerIdNum = 0;
    
    for i in 1L .. perWorkerSegment .. N do
        // printfn "%d" workerIdNum
        // printfn "workerIdNum = %d" workerIdNum
        workersList.Item(workerIdNum) <! WorkerMessage(i, min N (i+perWorkerSegment-1L), k)
        // printfn "%d to %d" i (min N (i+perWorkerSegment-1L))
        workerIdNum <- workerIdNum + 1
    // printfn "workerIdNum = %d" workerIdNum


let SupervisorActor (mailbox: Actor<_>) = 
    let mutable finishedCount  = 0

    let rec loop () = actor {
        let! msg = mailbox.Receive ()
        // printfn "%d %d" N k
        match msg with
        | BossMessage(N, k) -> 
            supervisorHelper N k
        | WorkerTaskFinished(st,en) -> 
            printfn "%d" st 
            // if finishedCount = totalRunningWorkers(N_copy) then
            // printfn ""
            // resList.Add(st)
            // listPrinter r/esList
        return! loop ()
    }
    loop ()


let main(args: array<string>) = 
    let N = int64(args.[3])
    let k = int64(args.[4])
    let actorRef = spawn system "SupervisorActor" SupervisorActor
    // printfn "the value of N: %d" N
    // printfn "the value of k: %d" k
    N_copy <- N
    printfn "total running workers = %d" (totalRunningWorkers N_copy)
    actorRef <! BossMessage(N, k)
    System.Console.ReadKey() |> ignore
    // printfn "chacha!"
    

main(Environment.GetCommandLineArgs())