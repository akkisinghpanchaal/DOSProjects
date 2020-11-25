module ApiMsgs

type ServerApi = 
    | SignUp of string * string

type ClientApi =
    | Init of string