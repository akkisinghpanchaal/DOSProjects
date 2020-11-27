#load @"user.fs"
#load @"tweet.fs"
#load @"messages.fs"
#load @"server.fsx"
#load @"client.fsx"

#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 


open ApiMsgs
open System
open ClientMod
open Akka.FSharp

module DriverMod =
    let randomStr = 
        let chars = "abcdefghijklmnopqrstuvwxyz"
        let charsLen = chars.Length
        let random = System.Random()

        fun len -> 
            let randomChars = [|for i in 0..len -> chars.[random.Next(charsLen)]|]
            System.String(randomChars)
    

    let main(args: array<string>) =
        // let n,r = int(args.[3]),int(args.[4])
        // let mutable errorFlag = false
        // numNodes <-n
        // numTweets <-r
        let mutable clientActor = spawn system "client" Client
        let mutable clientActor2 = spawn system "client2" Client
        clientActor <! Register("akkisingh","1234")
        clientActor2 <! Register("rajat.rai","1234")
        clientActor <! Login
        clientActor2 <! Login
        clientActor <! SendTweet("balle balle * 100 #lazymonday @rajat.rai @bodambasanti")
        clientActor <! SendTweet("#1231 @123 @assd")
        clientActor2 <! SendTweet("#1231 #123123 #asdkfmewsdf")
        clientActor2 <! FollowUser("akkisingh")
        clientActor <! FollowUser("rajat.rai")
        clientActor <! SendReTweet("@akkisingh ki gaand me danda", 1)

        System.Threading.Thread.Sleep(500)
        serverActor <! ShowData


    main(Environment.GetCommandLineArgs())
    System.Console.ReadKey() |> ignore