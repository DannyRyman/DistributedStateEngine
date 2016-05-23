namespace Tests

open Xunit
open FsUnit.Xunit
open Raft
open TimerLibrary
open Tests.Logging
open Tests.Fakes
open System.Threading

type RaftServerTests(testOutputHelper) =          
  let fakeElectionTimeoutService = new FakeTimeoutService()
  let fakeHeartbeatTimeoutService = new FakeTimeoutService()
  let nullWorkflow = new NullWorkflow()
    
  let loggerConfig = createTestLoggerConfig testOutputHelper 
  
  let createServerAndStart() =
    let server = new Server(fakeElectionTimeoutService, fakeHeartbeatTimeoutService, nullWorkflow, loggerConfig)
    let serverStartedLatch = new AutoResetEvent(false)
    let cancellationTokenSource = new CancellationTokenSource()
    server.Start cancellationTokenSource.Token |> Async.Start
    server.ServerEvent.Add(fun x -> 
      match x with
      | ServerStarted -> serverStartedLatch.Set() |> ignore
    )
    serverStartedLatch.WaitOne() |> ignore
    server

  [<Fact>]
  let ``when server started, context must be in expected state (no previous persisted state)`` () =
    let state = createServerAndStart().GetState()
    let followerState = 
      match state with 
      | Follower x -> x
      | Candidate _ | Leader -> failwith "Unexpected state"
    followerState.CurrentTerm |> should equal 0UL
    followerState.PreviousLogIndexes |> should equal None

  [<Fact>]
  let ``when server started, must start the election timeout service`` () =
    let server = createServerAndStart()
    fakeElectionTimeoutService.RecordedValues.Count |> should equal 1
    fakeElectionTimeoutService.RecordedValues.Item 0 |> should equal Start


    


    


    

    
