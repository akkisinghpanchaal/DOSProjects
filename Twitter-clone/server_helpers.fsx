#load @"messages.fs"
#load @"global_data.fs"
#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 

open System

open Akka.Actor
open Akka.FSharp

open ApiMsgs
open GlobalDataMod

