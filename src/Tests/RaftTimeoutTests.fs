namespace Tests

open Raft.StateTransitions
open Xunit
open System.Threading
open Raft.Workflow
open Raft
open FsUnit.Xunit
open Tests.Logging
open Fakes

type RaftTimeoutTests(testOutputHelper) =          
  let fakeElectionTimeoutService = new FakeTimeoutService()
  let fakeHeartbeatTimeoutService = new FakeTimeoutService()
  let loggerConfig = createTestLoggerConfig testOutputHelper 

  [<Fact>]
  let ``on election timeout, must pass an election timeout to the workflow`` () =    
    let latch = new AutoResetEvent(false)
    let mutable raftEvent:Option<RaftEvent> = None
    let fakeWorkflow (initialContext:Context, e) = 
      raftEvent <- Some e
      latch.Set() |> ignore
      initialContext
     
    let server = new Server(fakeElectionTimeoutService, fakeHeartbeatTimeoutService, fakeWorkflow, loggerConfig)
    server.Start() |> Async.RunSynchronously    
    fakeElectionTimeoutService.TriggerTimeout()
    let wasTripped = latch.WaitOne(1000)
    wasTripped |> should equal true
    raftEvent |> should equal (Some ElectionTimeout)