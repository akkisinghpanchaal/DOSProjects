#time "on"
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 

open System
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Akka.TestKit
open System.Collections.Generic

let totalWorkers = 100
let resSet = Set.empty
let mutable N_copy:int64 = 0L
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

let WorkerActor (mailbox: Actor<_>) =
    let rec loop() = actor {
        let! WorkerMessage(st, en, k) = mailbox.Receive()
        for firstNum = st to en do
            if isSumPerfectSquare firstNum (firstNum+k-1L) then
                mailbox.Sender() <! WorkerTaskFinished(firstNum, firstNum+k-1L)
        return! loop()
    }
    loop()


let supervisorHelper (N:int64) (k: int64) = 
        
    let perWorkerSegment = int64(ceil(float N / float totalWorkers))
    
    let workersList = List.init totalWorkers (fun workerId -> spawn system (string workerId) WorkerActor)
    let mutable workerIdNum = 0;
    
    for i in 1L .. perWorkerSegment .. N do
        workersList.Item(workerIdNum) <! WorkerMessage(i, min N (i+perWorkerSegment-1L), k)
        workerIdNum <- workerIdNum + 1

let SupervisorActor (mailbox: Actor<_>) = 
    let mutable finishedCount  = 0

    let rec loop () = actor {
        let! msg = mailbox.Receive ()
        match msg with
        | BossMessage(N, k) -> 
            supervisorHelper N k
        | WorkerTaskFinished(st,en) -> 
            printfn "%d" st
            
        return! loop ()
    }
    loop ()


let main(args: array<string>) = 
    let N = int64(args.[3])
    let k = int64(args.[4])
    N_copy <- N
    let actorRef = spawn system "SupervisorActor" SupervisorActor
    actorRef <! BossMessage(N, k)
    System.Console.ReadKey() |> ignore
    system.Terminate

main(Environment.GetCommandLineArgs())