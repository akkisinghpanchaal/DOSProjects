module CustomTypesMod

type QueryType = 
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


type ApiDataResponse(response: string, status: bool, data: array<string * string>) =
    let responseMsg = response
    let responseStatus = status
    let responseData = data

    member this.Response
        with get() = responseMsg

    member this.Status
        with get() = responseStatus
    
    member this.Data
        with get() = responseData

// type ApiResponse = {
//     response: string
//     status: bool
// }

// type ApiDataResponse = {
//     response: string
//     status: bool
//     data: array<string * string>
// }

// type BandaBanao = {
//     username: string
//     password: string
// }


type ServerApi = 
    | SignUp of string * string
    | SignIn of string * string
    | SignOut of string
    | RegisterTweet of string * string
    | RegisterReTweet of string * string * int
    | Follow of string * string
    | FindMentions of string 
    | FindSubscribed of string
    | FindHashtags of string * string
    | ShowData 
    | Testing

type ClientApi =
    | Login
    | Logout
    | GetMentions
    | GetSubscribed
    | GetHashtags of string
    | SendTweet of string
    | SendReTweet of string * int
    | Register of string * string
    | FollowUser of string
    | LiveFeed of string * string
    | ApiResponse of string * bool
    | ApiDataResponse of string * bool * array<string * string>
    