module ApiMsgs

type ServerApi = 
    | SignUp of string * string
    | SignIn of string * string
    | SignOut of string
    | RegisterTweet of string * string
    | SendReTweet of string * string
    | Follow of string * string
    | FindTweets of string
    | ShowData 

type ClientApi =
    | Login
    | SendTweet of string
    | Response of string
    | Register of string * string
    | FollowUser of string * string
