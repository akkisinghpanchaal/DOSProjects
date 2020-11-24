module TwitterClone

type GlobalData() =
    let mutable connectedServers = 0
    let mutable users = 0
    let mutable tweets = 0
    let createUserInterval = 1000 //1000 milliseconds
    let createTweetInterval = 1000 //1000 milliseconds
    let createFollowerInterval = 0

    let privateAddConnectedServers count = 
        connectedServers <- connectedServers + count

    let privateAddTweets count = 
        tweets <- tweets + count
    
    let privateAddUsers count = 
        users <- users + count

// =================== methods =======================
    member this.ConnectedServers
        with get() = connectedServers
        // and set(count) = connectedServers <- connectedServers + count

    member this.Users
        with get() = users
        // and set(count) = users <- users + count

    member this.Tweets
        with get() = tweets
        // and set(count) = tweets <- tweets + count

    member this.AddServers count =
        privateAddConnectedServers count
    
    member this.AddUsers count =
        // privateAddUsers count
        users <- users + count
    
    member this.AddTweets count =
        privateAddTweets count

// ----------------------------------------------------------
