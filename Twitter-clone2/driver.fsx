#load @"custom_types.fs"
#load @"server.fsx"
#load @"client.fsx"

#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit"


open System

open Akka.Actor
open Akka.FSharp
open CustomTypesMod
open ServerMod
open ClientMod

let zipfErrorPct = 10.00
let random = System.Random()

let precisionVal (num: int) (precisionPct: float) =
    // provides a value which is within a precision of (precisionPct %) from num
    // for example a sample value within 10% precision 100 could be 104 or 97.

    let random = System.Random()
    let low = int(float(num) * ((100.00 - precisionPct)/100.00))
    let hi = int(float(num) * ((100.00 + precisionPct)/100.00))
    random.Next(low, hi)

let nextZipfSize currFollowingSize = 
    // In case the following size come to be 0, max will pull it up to 1 which we assume
    // as the least size of subscibersip for any user.
    max 1 (precisionVal (currFollowingSize/2) zipfErrorPct)


let randomStr = 
    let chars = "abcdefghijklmnopqrstuvwxyz"
    let charsLen = chars.Length

    fun len -> 
        let randomChars = [|for i in 0..len -> chars.[random.Next(charsLen)]|]
        System.String(randomChars)


let getRandomUser (currentUser:int) (totalUsers: int): int =
    // get random user index other than the current user from the system
    let mutable randUserNum = random.Next(totalUsers)
    while randUserNum = currentUser do
        randUserNum <- random.Next(totalUsers)
    randUserNum


let Emulator (mailbox: Actor<_>) =
    let mutable totalUsers,maxSubscribers,maxTweets,tasksCount,tasksDone=0,0,0,0,0
    let rec loop() = actor {
        let! msg = mailbox.Receive()
        match msg with
        | Init(totalUsrs, maxSubs, maxTwts) ->
            let mutable totalUsers, maxSubscribers, maxTweets, totalTasks = totalUsrs, maxSubs, maxTwts , 0
            let idSize = string(totalUsers).Length
            if maxSubscribers > totalUsers then
                printfn "[Warning] Max subscribers cannot be more than the total numbers in the system."
                maxSubscribers <- totalUsers-1
            printfn "Total Users: %d\nMax Subscribers: %d" totalUsers maxSubscribers
            
            let makeUserId (idNum:int): string = String.concat "" ["user"; String.replicate (idSize - string(idNum).Length) "0"; string(idNum)]
            let userActors = List.init totalUsers (fun idNum -> spawn system (makeUserId(idNum)) Client)
            System.Threading.Thread.Sleep(100)

            for i in 0..(userActors.Length-1) do
                userActors.[i] <! Register(makeUserId(i),randomStr 12)
            totalTasks<-userActors.Length*3
            // giving some time for signing up all the users
            System.Threading.Thread.Sleep(500)

            // login all the users signed up
            // this could be changed to simulate periodic active span of users.
            // for i in 0..(userActors.Length-1) do
            
            // creating a zipf distribution of subscribers.
            // The max count of subscribers is passed from the command line
            let mutable followingSize, tweetsCap, userStr, tweetCount, retweetCount = maxSubscribers, maxTweets, "",0,0
            let mutable currFollowing: Set<int> = Set.empty
            for userNum in 0..(totalUsers-1) do
                userStr <-(makeUserId(userNum))
                printfn "User %s will get %d followers" userStr followingSize
                userActors.[userNum] <! Login
                for _ in 1..followingSize do
                    let mutable randUser = getRandomUser userNum totalUsers
                    // check if the user is already followed
                    while currFollowing.Contains randUser do
                        randUser <- getRandomUser userNum totalUsers
                    currFollowing <- currFollowing.Add(randUser)
                    userActors.[userNum] <! FollowUser(makeUserId(randUser))
                System.Threading.Thread.Sleep(100)
                totalTasks <-  followingSize + totalTasks
                currFollowing <- Set.empty
                followingSize <- nextZipfSize followingSize
            System.Threading.Thread.Sleep(300)
        
            for userNum in 0..(totalUsers-1) do
                userStr <- (makeUserId(userNum))
                printfn "User %s will tweet %d times" userStr tweetsCap
                for _ in 1..tweetsCap do
                    let mutable randUser = getRandomUser userNum totalUsers
                    // check if the user is already followed
                    while currFollowing.Contains randUser do
                        randUser <- getRandomUser userNum totalUsers
                    currFollowing <- currFollowing.Add(randUser)
                    userActors.[userNum] <! SendTweet("@"+string(makeUserId(randUser))+ " "+randTweets.[ random.Next() % randTweets.Length ])
                    if (userNum%2 <> 0) <> (tweetsCap%2 <>0) then
                        userActors.[userNum] <! SendReTweet("@"+string(makeUserId(randUser))+ " "+randTweets.[ random.Next() % randTweets.Length ], random.Next()%tweetCount)
                        retweetCount <- retweetCount + 1
                tweetCount <- tweetsCap + tweetCount
                tweetsCap <- nextZipfSize tweetsCap
                System.Threading.Thread.Sleep(100)
            tasksCount <- totalTasks + retweetCount
            printfn "TOTAL TASKS/COMPLETEd :%d %d" totalTasks tasksDone
        | UnitTaskCompleted -> 
            tasksDone<- 1+ tasksDone
            printfn "Done!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
            if tasksDone = tasksCount then 
                printfn "Done!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"

        return! loop()
    }
    loop()

let main(args: array<string>) =
    let totalUsers, maxSubscribers, maxTweets = int(args.[3]), int(args.[4]), int(args.[5])
    let emulatorActor = spawn system "emulator" Emulator
    emulatorActor <! Init(totalUsers,maxSubscribers,maxTweets)
    // let task = emulatorActor <? Init(totalUsers,maxSubscribers,maxTweets)
    // let response = Async.RunSynchronously (task, 1000)
    // printfn "Reply from remote %s" (string(response))

main(Environment.GetCommandLineArgs())
System.Console.ReadKey() |> ignore