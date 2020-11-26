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
        // let chars = "ABCDEFGHIJKLMNOPQRSTUVWUXYZ0123456789"
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
        let str = randomStr(10)
        printfn "rand : %s" str
        clientActor <! SendTweet(str)
        clientActor2 <! SendTweet(str)
        // serverActor <! ShowData
        // serverActor <! SignOut("rajat.rai")
        System.Threading.Thread.Sleep(3000)
        serverActor <! ShowData


    main(Environment.GetCommandLineArgs())
    System.Console.ReadKey() |> ignore