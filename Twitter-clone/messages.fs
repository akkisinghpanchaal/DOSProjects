module ApiMsgs

type ServerApi = 
    | SignUp of string * string
    | SignIn of string * string
    | SignOut of string
    | RegisterTweet of string * string
    | RegisterReTweet of string * string * int
    | Follow of string * string
    | FindTweets of string
    | ShowData 

type ClientApi =
    | Login
    | SendTweet of string
    | SendReTweet of string * int
    | Response of string * bool
    | Register of string * string
    | FollowUser of string
