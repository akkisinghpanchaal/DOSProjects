module GlobalDataMod

#load @"user.fs"
#load @"tweet.fs"

open UserMod
open TweetMod

type GlobalData() =
    let mutable connectedServers = 0
    let mutable users: Map<string, User> = Map.empty
    let mutable tweets: Map<int, Tweet> = Map.empty
    let mutable hashtags: Map<string, int[]> = Map.empty
    let mutable loggedInUsers: Set<string> = Set.empty

    let createUserInterval = 1000 //1000 milliseconds
    let createTweetInterval = 1000 //1000 milliseconds
    let createFollowerInterval = 0

    let privateAddConnectedServers count = 
        connectedServers <- connectedServers + count    

    let privateAddTweets (tweetId:int) (tweet: Tweet) = 
        tweets <- tweets.Add(tweetId,tweet)
    
    let privateAddUsers (username:string) (userObj: User) = 
        users <- users.Add(username, userObj)

    let markUserLoggedIn (username: string) =
        loggedInUsers <- loggedInUsers.Add(username)

    let markUserLoggedOut (username: string) =
        loggedInUsers <- loggedInUsers.Remove(username)
    
// =================== methods =======================
    member this.ConnectedServers
        with get() = connectedServers
        // and set(count) = connectedServers <- connectedServers + count

    member this.Users
        with get() = users
        // and set(newMap) = users <- newMap

    member this.Tweets
        with get() = tweets

    member this.LoggedInUsers
        with get() = loggedInUsers

    member this.AddServers count =
        privateAddConnectedServers count
    
    member this.AddUsers count =
        privateAddUsers count
    
    member this.AddTweets (tweetid:int) (payload:Tweet) =
        privateAddTweets tweetid payload

    member this.MarkUserLoggedIn username =
        markUserLoggedIn username
    
    member this.MarkUserLoggedOut username =
        markUserLoggedOut username
// ----------------------------------------------------------
