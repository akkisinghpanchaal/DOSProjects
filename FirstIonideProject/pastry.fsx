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
let mutable actorHopsMap: Map<string, double[]> = Map.empty
let mutable srcdst:Map<String,String> = Map.empty

type BossMessage = 
    | BossMessage of int
    | WorkerTaskFinished of int

type WorkerMessage = 
    | Init of string
    | Join of string * int
    | Route of string * string * double
    | UpdateRoutingTable of string[]
    | ShowTable
    | ShowLeaf of string * Set<string>
    | ShowStr of string

let PrinterActor (mailbox: Actor<_>) =
    let rec loop() = actor {
        let! msg = mailbox.Receive()
        match msg with
        | ShowLeaf(nodeId, leafSet) ->
            printf "For node %s len:%d = " nodeId leafSet.Count
            for e in leafSet do
                printf "%s " e
            printfn ""
        | ShowStr(str) ->
            printfn "%s" str
        | _ ->
            printf ""
        return! loop()
    }
    loop()

let mutable printer = spawn system "PrinterActor" PrinterActor

let NodeActor (mailbox: Actor<_>) = 
    // count keeps track of all the workers that finish their work and ping back to the supervisor
    // *****************************************
    let mutable id: string = ""
    let cols = l
    let mutable leafSet: Set<string> = Set.empty
    let mutable neighborSet: Set<string> = Set.empty
    let mutable routingTable: string[,] = Array2D.zeroCreate numDigits l
    let mutable currentRow = 0
    let mutable temp = 0
    let rec loop () = actor {
        let! msg = mailbox.Receive ()
        match msg with
            // Initialization phase of a network node
            | Init(passedId) ->
                id <- passedId
                let number = Convert.ToInt32(id, l)
                for i in [1..l/2] do
                    if number-i<0 then 
                        leafSet <- leafSet.Add((numNodes-i).ToString("X"))
                    else
                        leafSet <- leafSet.Add((number-i).ToString("X"))
                    if number+i>=numNodes then 
                        leafSet <- leafSet.Add((number+i-numNodes).ToString("X"))
                    else
                        leafSet <- leafSet.Add((number+i).ToString("X"))

            // Updates routing table for a new node
            | Join (nodeId, currentIndex) ->
                let mutable i = 0
                let mutable k = currentIndex
                // keep incrementing counter while same characters are encountered in the hex node IDs
                while nodeId.[i] = id.[i] do
                    i <- i + 1
                let sharedPrefixLength = i
                let mutable routingRow = Array.zeroCreate l
                while k<=sharedPrefixLength do
                    let mutable routingRow: string[] = Array.init l (fun x -> routingTable.[k,x])
                    routingRow.[Convert.ToInt32(string(id.[sharedPrefixLength]),l)] <- id
                    actorMap.[nodeId] <! UpdateRoutingTable(routingRow)
                    k <- k + 1
                let rtrow = sharedPrefixLength
                let rtcol = Convert.ToInt32(string(nodeId.[sharedPrefixLength]),l)
                if isNull routingTable.[rtrow,rtcol] then
                    routingTable.[rtrow,rtcol] <- nodeId
                else
                    actorMap.[routingTable.[rtrow,rtcol]] <! Join(nodeId,k)
            // Update the current row of the routing table with the given row
            | UpdateRoutingTable(routingRow) ->
                routingTable.[currentRow,*] <- routingRow
                currentRow <- currentRow + 1
            // Routes a message with destination as key from source where hops is the hops traced so far
            | Route(key, source, hops) ->
                printer <! ShowStr(sprintf "ThisNodeID: %s\n%s -> %s\n" id source key)
                printer <! ShowStr(sprintf "%A\n" routingTable)
                if key = id then
                    if actorHopsMap.ContainsKey source then
                        printer <! ShowStr(sprintf "REACHED!! YIPEE;  Hops %A\n" hops)
                        let total, avgHops = actorHopsMap.[source].[1], actorHopsMap.[source].[0]
                        actorHopsMap.[source].[0] <- ((avgHops*total)+hops)/(total+1.0)
                        actorHopsMap.[source].[1] <- total + 1.0
                    else
                        printf "actorsHop dsnt have the key\n"
                        let mutable tempArr = [|hops;1.0|]
                        actorHopsMap <- actorHopsMap.Add (source, tempArr)
                        printer <! ShowStr(sprintf "actors Hop map No ERR")
                elif leafSet.Contains key then
                    printer <! ShowStr(sprintf "Second If Case HERE\n")
                    actorMap.[key] <! Route(key, source, hops + 1.0)
                else
                    printer <! ShowStr(sprintf "Third If Case HERE \n")
                
                    let mutable i = 0
                    while key.[i] = id.[i] do
                        i <- i + 1
                    let sharedPrefixLength = i
                    let check = 0
                    let rtrow = sharedPrefixLength
                    printer <! ShowStr(sprintf "NumberOfDigit: %d\n Shared Prefix Len : %d \n" numDigits sharedPrefixLength)
                    let mutable rtcol = Convert.ToInt32(string(key.[sharedPrefixLength]), l)
                    printer <! ShowStr(sprintf "row: %d | col: %d | routingTable.[rtrow,rtcol]: %s" rtrow rtcol routingTable.[rtrow,rtcol])
                    if not (isNull routingTable.[rtrow, rtcol]) then
                        printer <! ShowStr(sprintf "Special Case HERE for rtrow rtcol\n")
                        actorMap.[routingTable.[rtrow,rtcol]] <! Route(key, source, hops+1.0)
                    else
                        printer <! ShowStr(sprintf "Special Case HERE inside else for next node\n")
                        let mutable dist = 0
                        let mutable distMin = 2147483647
                        let mutable nextNodeId = ""
                        let mutable shl = 0

                        for candidateNode in leafSet do
                            printer <! ShowStr(sprintf "sab thik 1")
                            while candidateNode.[shl] = id.[shl] do 
                                shl <- shl + 1
                            printer <! ShowStr(sprintf "sab thik 2")
                            if shl >= sharedPrefixLength then
                                dist <- (Convert.ToInt32(candidateNode, l) -  Convert.ToInt32(id, l))
                                if dist < distMin then
                                    nextNodeId <- candidateNode
                            shl <- 0
                        printer <! ShowStr(sprintf "nextNodeId: %s" nextNodeId)
                        actorMap.[nextNodeId] <! Route(key, source, hops + 1.0)
                        printer <! ShowStr(sprintf "Special Case HERE for next Node id\n")

            | ShowTable ->
                printer <! ShowLeaf(id,leafSet)
                // printfn "Agaya"
                // for e in leafSet do
                //     printf "%s " e
                // printfn "__________________"
                // for i = 0 to numDigits-1 do
                //     // for j = 0 to l-1 do
                //     //     printf "%s " routingTable.[i,j]
                //     printfn "%A" routingTable.[i,*]
                // printfn "============================================================="
            |_ ->
                printfn "Error!\ns"
            
        return! loop ()
    }
    loop ()

let Keys(map: Map<'K,'V>) =
    seq {
        for KeyValue(key,value) in map do
            yield key
    } |> List.ofSeq

let main(args: array<string>) = 
    let n,r = int(args.[3]),int(args.[4])
    let mutable errorFlag = false
    numNodes <-n
    numRequests <-r
    numDigits <- Math.Log(numNodes|> double, 16.) |> ceil |> int
    printf "N:%d\nR:%d\nNumber of digits:%d\n" numNodes numRequests numDigits
    printf "Network construction initiated"
    let mutable nodeId:string=String.replicate numDigits "0"
    let mutable hexNum:string=""
    let mutable len = 0
    printf "Node Id: %s\n" nodeId
    let mutable actor = spawn system nodeId NodeActor
    actor <! Init nodeId
    actorMap <- actorMap.Add(nodeId,actor)
    for i in 1 .. numNodes-1 do
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
        printf "\nNode creating %s\n" nodeId
        actor <- spawn system (string nodeId) NodeActor
        actor <! Init(nodeId)
        actorMap <- actorMap.Add(nodeId,actor)
        actorMap.[String.replicate numDigits "0"] <! Join(nodeId,0)
        System.Threading.Thread.Sleep(100)
    
    if true then
        for i in [0..numNodes-1] do
            hexNum <- i.ToString("X")
            len <- hexNum.Length
            nodeId <- String.concat  "" [String.replicate (numDigits-len) "0"; hexNum]
            // printfn "Ye table %s ka hai" (i.ToString("X"))
            actorMap.[nodeId] <! ShowTable
            System.Threading.Thread.Sleep(100)

    printf "\nNetwork is built!!!\n"
    let actorsArray = Keys(actorMap)
    // for (KeyValue(k, v)) in actorMap ->

    // printf  "ActorMap Keys :%s \n" Map.iter actorMap.[]
    printf  "ActorMap Keys :%A\nlength : %d\n" actorsArray actorsArray.Length
    printf  "ActorMap Keys :%s \n" actorsArray.[numNodes-1]
    let mutable src,dst= null, null

    printfn "Waiting for 10 seconds...."
    System.Threading.Thread.Sleep(5000)
    
    // for actmapping in actorMap do
    //     printfn "%s | %A" actmapping.Key actmapping.Value

    // System.Threading.Thread.Sleep(10000)

    for i in 0..numRequests-1 do
        src <- actorsArray.[i%actorsArray.Length] 
        dst <- actorsArray.[rnd.Next()%actorsArray.Length]
        while dst = src do
            dst <- actorsArray.[rnd.Next()%actorsArray.Length]
        actorMap.[src] <! Route(dst,src,0.0)
        System.Threading.Thread.Sleep(300)
        printf "Rand src:%s dst:%s\n" src dst
    
main(Environment.GetCommandLineArgs())
// If not for the below line, the program exits without printing anything.
// Please press any key once the execution is done and CPU times have been printed.
// You might have to scroll and see the printed CPU times. (Because of async processing)
System.Console.ReadKey() |> ignore
