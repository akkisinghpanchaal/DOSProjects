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

    let privateAddHashtags (tweet: Tweet) = 
        for h in tweet.Hashtags do
            if hashtags.ContainsKey(h) then
                let updatedIds = Array.append hashtags.Item(h) [|tweet.Id|]
                hashtags <- hashtags.Change(h,updatedIds)
            else
                hashtags <- hashtags.Add(h,[|tweet.Id|])
        //call private add mentions

    let privateAddTweets (tweet: Tweet) = 
        tweets <- tweets.Add(tweet.Id,tweet)
        privateAddHashtags tweet
    
    let privateAddUsers (username:string) (userObj: User) = 
        users <- users.Add(username, userObj)

    let markUserLoggedIn (username: string) =
        loggedInUsers <- loggedInUsers.Add(username)

    let markUserLoggedOut (username: string) =
        loggedInUsers <- loggedInUsers.Remove(username)
    
// =================== methods =======================
    member this.ConnectedServers
        with get() = connectedServers

    member this.Users
        with get() = users

    member this.Tweets
        with get() = tweets

    member this.LoggedInUsers
        with get() = loggedInUsers

    member this.AddServers count =
        privateAddConnectedServers count
    
    member this.AddUsers count =
        privateAddUsers count
    
    member this.AddTweet (payload:Tweet) =
        privateAddTweets payload

    member this.MarkUserLoggedIn username =
        markUserLoggedIn username
    
    member this.MarkUserLoggedOut username =
        markUserLoggedOut username
// ----------------------------------------------------------
