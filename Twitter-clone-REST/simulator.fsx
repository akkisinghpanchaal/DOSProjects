#load @"custom_types.fs"
#load @"client.fsx"

#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit"
#r "nuget: MathNet.Numerics"
open System

open Akka.Actor
open Akka.FSharp
open CustomTypesMod
open ClientMod
open MathNet.Numerics

let zipfErrorPct = 10.00
let random = System.Random()
// let system = ActorSystem.Create("TwitterClient")

let precisionVal (num: int) (precisionPct: float) =
    // provides a value which is within a precision of (precisionPct %) from num
    // for example a sample value within 10% precision 100 could be 104 or 97.

    let random = System.Random()
    let low = int(float(num) * ((100.00 - precisionPct)/100.00))
    let hi = int(float(num) * ((100.00 + precisionPct)/100.00))
    random.Next(low, hi)

let nextZipfSize currFollowingSize = 
    // In general, the lower bound of this method is 1.
    // For instance, in case when the following size comes to be 0, max will pull it up to 1 which we assume
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

let getZipfDistribution (totalUsers: int)  (maxSubscribers: int) =
    let mutable initialDist: int array = Array.zeroCreate totalUsers
    Distributions.Zipf.Samples(initialDist, 1.5, maxSubscribers) 
    Array.sortBy (fun x -> (-x)) initialDist

let Simulator (mailbox: Actor<_>) =
    let mutable totalUsers=0
    let mutable maxSubscribers = 0
    let mutable maxTweets = 0
    let mutable tasksCount = 0
    let mutable tasksDone = 0
    let mutable stopWatch = null
    let mutable randomRetweetCount = 0
    let mutable randomLogoutCount = 0
    let mutable topProfilesCount = 0
    let mutable followingSizesZipf: int array = Array.empty
    let mutable tweetsDistZipf: int array = Array.empty
    let mutable tweetCountUsers: int array = Array.empty
    let mutable userActors: List<IActorRef> = List.empty
    let mutable caller: IActorRef = null

    let makeUserId (idNum:int): string = 
        let idSize = string(totalUsers).Length
        String.concat "" ["user"; String.replicate (idSize - string(idNum).Length) "0"; string(idNum)]
    
    let rec loop() = actor {
        let! msg = mailbox.Receive()
        match msg with
        | Init(totalUsrs, maxSubs, maxTwts) ->
            caller <- mailbox.Sender()
            totalUsers <- totalUsrs
            maxSubscribers <- maxSubs
            maxTweets <- maxTwts
            let mutable totalTasks = 0

            if maxSubscribers > totalUsers then
                printfn "[Warning] Max subscribers cannot be more than the total number of users in the system.\n"
                maxSubscribers <- totalUsers-1
            printfn "Total Users: %d\nMax Subscribers: %d" totalUsers maxSubscribers
            
            userActors <- List.init totalUsers (fun idNum -> spawn system (makeUserId(idNum)) Client)

            // generating zipf distribution of followers for each users in order of id
            let followingSizes = getZipfDistribution totalUsers maxSubscribers
            followingSizesZipf <- followingSizes
            let tweetsDist = getZipfDistribution totalUsers maxTweets
            tweetsDistZipf <- tweetsDist

            printfn "\nStarting simulation...\n"
            stopWatch <- System.Diagnostics.Stopwatch.StartNew()
            for i in 0..(userActors.Length-1) do
                userActors.[i] <! Register(makeUserId(i),randomStr 12)
            totalTasks<-userActors.Length*2

            // giving some time for signing up all the users

            // login all the users signed up
            // this could be changed to simulate periodic active span of users.
            // for i in 0..(userActors.Length-1) do

            // creating a zipf distribution of subscribers.
            // The max count of subscribers is passed from the command line
            // System.Threading.Thread.Sleep(2000)    

            let mutable followingSize = 0
            let mutable userStr = ""
            let mutable tweetCount = 0
            let mutable retweetCount = 0
            let mutable loggedOut = 0
            let mutable currFollowing: Set<int> = Set.empty
            
            // userActors.[3] <! Login
            // printfn "login hua"
            // System.Threading.Thread.Sleep(2000)
            // printfn "follow bheja"
            // userActors.[3] <! FollowUser(makeUserId(4))

            for userNum in 0..(totalUsers-1) do
                // userStr <-(makeUserId(userNum))
                printfn "USER : %d" userNum
                followingSize <- followingSizes.[userNum]
                userActors.[userNum] <! Login
                
                // System.Threading.Thread.Sleep(1000)    
                
                for _ in 1..followingSize do
                    let mutable randUser = getRandomUser userNum totalUsers
                    // check if the user is already followed
                    while currFollowing.Contains randUser do
                        randUser <- getRandomUser userNum totalUsers
                    currFollowing <- currFollowing.Add(randUser)
                    printfn "%s following %s" (makeUserId(userNum)) (makeUserId(randUser))
                    userActors.[userNum] <! FollowUser(makeUserId(randUser))
                currFollowing <- Set.empty
            totalTasks <- Array.sum followingSizes + totalTasks

            printfn "34q5q444knrqlnqlnr;qknr================"
            
            // let mutable randUser = getRandomUser 0 totalUsers
            // let mutable randTweet = "@" + string(makeUserId(randUser)) + " " + randTweets.[random.Next(randTweets.Length)]
            // userActors.[0] <! SendTweet(randTweet)

            for userNum in 0..(totalUsers-1) do
                printfn "pehla"
                userStr <- (makeUserId(userNum))
                // for _ in 1..tweetsDist.[userNum] do
                printfn "dusra"
                System.Threading.Thread.Sleep(300)    
                let mutable randUser = getRandomUser userNum totalUsers
                let mutable randTweet = "@" + string(makeUserId(randUser)) + " " + randTweets.[random.Next(randTweets.Length)]
                userActors.[userNum] <! SendTweet(randTweet)
                    // let syncTweet = userActors.[userNum] <? SendTweet(randTweet)
                    // let sresp = Async.RunSynchronously(syncTweet)
                    // printfn "%A" sresp
                    // if userNum%5 = random.Next(5) then
                    //     userActors.[userNum] <! SendReTweet("@"+string(makeUserId(randUser))+ " "+randTweets.[ random.Next() % randTweets.Length ], random.Next()%tweetCount)
                    //     retweetCount <- retweetCount + 1
                // if (userNum % 10) = (random.Next(10)) then
                //     loggedOut <- 1+ loggedOut
                //     userActors.[userNum] <! Logout
                    // printfn "User %s has logged out successfully." userStr
                // tweetCountUsers.[userNum] <- tweetsDist.[userNum]
                tweetCountUsers.[userNum] <- tweetsDist.[userNum]
                tweetCount <- tweetsDist.[userNum] + tweetCount
            randomRetweetCount <- retweetCount
            randomLogoutCount <- loggedOut
            tasksCount <- totalTasks + tweetCount + retweetCount + loggedOut
            printfn "Total Tasks:%d %d\nRetweet Count %d" totalTasks tasksDone retweetCount
            let temp = sprintf "Total Tasks:%d %d\nRetweet Count %d" totalTasks tasksDone retweetCount
            mailbox.Sender() <! temp
        | UnitTaskCompleted -> 
            topProfilesCount <- min 9 (totalUsers-1)
            let summaryTitle = "\n\n====================================\n\tSIMULATION SUMMARY\n====================================\n\n"
            let mutable summaryBody = 
                sprintf "Total Users: %d\nMax subscribers for any user: %d\nTotal API Requests processed: %d\nTotal random retweets: %d\nRandom logouts simulated: %d\nTotal simulation time: %.2f milliseconds" totalUsers maxSubscribers tasksCount randomRetweetCount randomLogoutCount stopWatch.Elapsed.TotalMilliseconds
            summaryBody <- summaryBody + "\n\nBelow are the top " + string(topProfilesCount) + " most popular accounts based on our simulation results.\n\n"

            for i in 0..topProfilesCount do
                let mutable thisAccountSummary = 
                    sprintf "%d. %s | Followers: %d | Tweets: %d\n" (i+1) (makeUserId(i)) (followingSizesZipf.[i]) (tweetCountUsers.[i])
                summaryBody <- summaryBody + thisAccountSummary
            
            caller <! (summaryTitle + summaryBody)
            return! loop()
        // | RunQuerySimulation ->
        //     printfn "Starting query simulation..."
        //     for i in 0..topProfilesCount do
        //         if i % 5 = 0 then
        //             userActors.[i] <! GetHashtags("#Christmas")
        //         elif i % 3 = 0 then
        //             userActors.[i] <! Login
        //             userActors.[i] <! GetSubscribed
        //         elif i % 2 = 0 then
        //             userActors.[i] <! Login
        //             userActors.[i] <! GetMentions
        return! loop()
    }
    loop()

let main(args: array<string>) =
    let totalUsers, maxSubscribers, maxTweets, runQuerySimulation = int(args.[3]), int(args.[4]), int(args.[5]), args.[6]
    let isRunQuerySimulation = ((runQuerySimulation.ToLower() = "yes") ||  (runQuerySimulation.ToLower() = "y"))
    let simulatorActor = spawn system "simulator" Simulator
    let task = simulatorActor <? Init(totalUsers,maxSubscribers,maxTweets)
    // let task = simulatorActor <? Init(10,10,10)
    let response = Async.RunSynchronously(task)
    printfn "%s" (string(response))
    // let anotherTask = simulatorActor <? UnitTaskCompleted
    // let anotherResponse = Async.RunSynchronously(anotherTask)
    // ()
    // If required, then perform query simulation as well
    // if isRunQuerySimulation then
    //     let task2 = simulatorActor <? RunQuerySimulation
    //     let response2 = Async.RunSynchronously (task2, 1000)
    //     printfn "Query simulation done."
main(Environment.GetCommandLineArgs())
// System.Console.ReadKey() |> ignore