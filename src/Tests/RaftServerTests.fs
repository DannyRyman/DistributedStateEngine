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
  let nullWorkflow (initialContext:Context, _) = 
      initialContext
    
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
    let context = createServerAndStart().GetContext()
    context.State |> should equal Follower
    context.CurrentTerm |> should equal 0UL
    context.PreviousLogIndexes |> should equal None

  [<Fact>]
  let ``when server started, must start the election timeout service`` () =
    let server = createServerAndStart()
    fakeElectionTimeoutService.RecordedValues.Count |> should equal 1
    fakeElectionTimeoutService.RecordedValues.Item 0 |> should equal Start


    


    


    

    
