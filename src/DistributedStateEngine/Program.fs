﻿open System
open System.Text
open fszmq
open fszmq.Context
open fszmq.Socket
open System.Threading
open Communication
open Configuration
open RaftImplementation

[<EntryPoint>]
let main argv = 
  printfn "libzmq version: %A" ZMQ.version
  let tokenSource = new CancellationTokenSource()
  let token = tokenSource.Token
  Async.Start (init token)
  Console.ReadLine() |> ignore
  tokenSource.Cancel()
  Console.ReadLine() |> ignore
  0 // return an integer exit code