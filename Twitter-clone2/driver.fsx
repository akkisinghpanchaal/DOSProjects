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


let precisionVal (num: int) (precisionPct: float) =
    // provides a value which is within a precision of (precisionPct %) from num
    // for example a sample value within 10% precision 100 could be 104 or 97.

    let rnd = System.Random()
    let low = int(float(num) * ((100.00 - precisionPct)/100.00))
    let hi = int(float(num) * ((100.00 + precisionPct)/100.00))
    rnd.Next(low, hi)

let nextZipfSize currFollowingSize = 
    // In case the following size come to be 0, max will pull it up to 1 which we assume
    // as the least size of subscibersip for any user.
    max 1 (precisionVal (currFollowingSize/2) zipfErrorPct)


let randomStr = 
    let chars = "abcdefghijklmnopqrstuvwxyz"
    let charsLen = chars.Length
    let random = System.Random()

    fun len -> 
        let randomChars = [|for i in 0..len -> chars.[random.Next(charsLen)]|]
        System.String(randomChars)


let getRandomUser (currentUser:int) (totalUsers: int): int =
    let rnd = System.Random()
    // get random user index other than the current user from the system
    let mutable randUserNum = rnd.Next(totalUsers)
    while randUserNum = currentUser do
        randUserNum <- rnd.Next(totalUsers)
    randUserNum


let main(args: array<string>) =
    let mutable totalUsers, maxSubscribers = int(args.[3]), int(args.[4])
    let idSize = string(totalUsers).Length

    if maxSubscribers > totalUsers then
        printfn "[Warning] Max subscribers cannot be more than the total numbers in the system."
        maxSubscribers <- totalUsers
    printfn "Total Users: %d\nMax Subscribers: %d" totalUsers maxSubscribers
    
    let makeUserId (idNum:int): string = 
        String.concat "" ["user"; String.replicate (idSize - string(idNum).Length) "0"; string(idNum)]
    

    // let system = ActorSystem.Create("Twitter")
    // let serverActor = spawn system "server" Server
    // let serverActor = spawn system "server" (Server printerActor)
    System.Threading.Thread.Sleep(500)
    
    // test user id generator
    // for i in [1..totalUsers] do
    //     printfn "%s" (makeUserId(i))
    let userActors = List.init totalUsers (fun idNum -> spawn system (makeUserId(idNum)) Client)
    // [for i in 0 .. (userActors.Length-1) do userActors.[i] <! Register(makeUserId(i),randomStr 12)] |> ignore
    for i in 0..(userActors.Length-1) do
        userActors.[i] <! Register(makeUserId(i),randomStr 12)

    // giving some time for signing up all the users
    System.Threading.Thread.Sleep(500)

    // login all the users signed up
    // this could be changed to simulate periodic active span of users.
    for i in 0..(userActors.Length-1) do
        userActors.[i] <! Login
    
    // creating a zipf distribution of subscribers.
    // The max count of subscribers is passed from the command line
    let mutable followingSize = maxSubscribers
    let mutable currFollowing: Set<int> = Set.empty
    for userNum in 0..(totalUsers-1) do
        printfn "User %s will get %d followers" (makeUserId(userNum)) followingSize
        for _ in 1..followingSize do
            let mutable randUser = getRandomUser userNum totalUsers
            // check if the user is already followed
            while currFollowing.Contains randUser do
                randUser <- getRandomUser userNum totalUsers
            currFollowing <- currFollowing.Add(randUser)
            userActors.[userNum] <! FollowUser(makeUserId(randUser))
            System.Threading.Thread.Sleep(100)
        currFollowing <- Set.empty
        followingSize <- nextZipfSize followingSize

    // printfn "idhar aao jara"
    // let mutable clientActor2 = spawn system "client2" (Client serverActor)
    // let mutable clientActor2 = spawn system "client2" (Client serverActor)
   
    // let mutable clientActor = spawn system "client" (Client serverActor)
    // clientActor <! Register("akkisingh","1234")

    // clientActor2 <! Register("rajat.rai","1234")
    // clientActor <! Login
    // clientActor2 <! Login
    // clientActor <! SendTweet("balle balle * 100 #lazymonday @rajat.rai @bodambasanti")
    // System.Threading.Thread.Sleep(1000)
    // clientActor <! SendTweet("#1231 @123 @assd")
    // System.Threading.Thread.Sleep(1000)
    // clientActor2 <! SendTweet("#1231 #123123 #asdkfmewsdf")
    // System.Threading.Thread.Sleep(1000)
    // clientActor2 <! FollowUser("akkisingh")
    // System.Threading.Thread.Sleep(1000)
    // clientActor <! FollowUser("rajat.rai")
    // clientActor <! SendReTweet("@akkisingh is a good boy", 1)
    // clientActor2 <! SendTweet("@rajat.rai is also a good boy!")
    // clientActor2 <! SendTweet("@akkisingh is of age 25")
    // clientActor2 <! SendTweet("@akkisingh likes to play badminton.")
    // clientActor2 <! SendTweet("@akkisingh bahar chalega ghumne??")
    // clientActor <! GetHashtags("asdkfmewsdf")
    // clientActor <! GetMentions
    // clientActor <! GetSubscribed
    // clientActor2 <! GetHashtags("asdkfmewsdf")
    // clientActor2 <! GetMentions
    // clientActor2 <! GetSubscribed
    // clientActor <! Logout
    // clientActor2 <! Logout
    // System.Threading.Thread.Sleep(500)
    // serverActor <! ShowData
    0

main(Environment.GetCommandLineArgs())
System.Console.ReadKey() |> ignore