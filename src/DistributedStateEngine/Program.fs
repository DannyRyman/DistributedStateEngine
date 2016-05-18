open System
open System.Text
open fszmq
open fszmq.Context
open fszmq.Socket
open System.Threading
open Communication
open Configuration
open RaftImplementation
open RaftState
open RaftTypes
open Raft
open TimerLibrary

[<EntryPoint>]
let main argv = 
  printfn "libzmq version: %A" ZMQ.version
  let tokenSource = new CancellationTokenSource()
  let token = tokenSource.Token
  
  let electionTimeoutService = new TimeoutService({MinimumTimeout = 200; MaximumTimeout = 300})
  
  let heartbeatTimeoutService = new TimeoutService({MinimumTimeout = 100; MaximumTimeout = 100})
  let server = new Server(electionTimeoutService, heartbeatTimeoutService)
  server.Start()
   
  (*
  Async.Start ( 
    async {
      let server = new Server()
      server.Start token
    })
  *)

  Console.ReadLine() |> ignore
  tokenSource.Cancel()
  Console.ReadLine() |> ignore
  0 // return an integer exit code