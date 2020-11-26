module ApiMsgs

type ServerApi = 
    | SignUp of string * string
    | SignIn of string * string
    | SignOut of string
    | SendTweet of string * string
    | SendReTweet of string * string
    | Follow of string * string
    | FindTweets of string
    | ShowData 

type ClientApi =
    | Init of string