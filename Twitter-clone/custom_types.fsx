module CustomTypesMod

#load @"tweet.fs"
open TweetMod

type QueryType = 
    | Hashtag
    | MyMentions
    | Subscribed

type ApiResponse(response: string, status: bool) =
    let responseMsg = response
    let responseStatus = status
    // let mutable responseData: Option<List<Tweet>> = match data with
    //                             | Some x -> x
    //                             | None -> null

    member this.Response
        with get() = responseMsg

    member this.Status
        with get() = responseStatus
    

type ApiDataResponse(response: string, status: bool, data: Tweet array) =
    let responseMsg = response
    let responseStatus = status
    let responseData = data

    member this.Response
        with get() = responseMsg

    member this.Status
        with get() = responseStatus
    
    member this.Data
        with get() = responseData


type ServerApi = 
    | SignUp of string * string
    | SignIn of string * string
    | SignOut of string
    | RegisterTweet of string * string
    | RegisterReTweet of string * string * int
    | Follow of string * string
    | FindTweets of string * string * QueryType
    | ShowData 


type ClientApi =
    | Login
    | SendTweet of string
    | SendReTweet of string * int
    | Response of ApiResponse
    | DataResponse of ApiDataResponse
    | Register of string * string
    | FollowUser of string
    | GetTweets of string * string * QueryType