namespace Tests

open Xunit
open FsUnit.Xunit
open Raft
open Raft.StateTransitions
open Tests.RaftFakes
open TimerLibrary

type RaftServerTests() =        
  
  let fakeElectionTimeoutService = new FakeTimeoutService()
  let fakeHeartbeatTimeoutService = new FakeTimeoutService()
  let server = new Server(fakeElectionTimeoutService, fakeHeartbeatTimeoutService)

  [<Fact>]
  let ``when server initialized, context must be in expected state (no previous persisted state)`` () =     
    server.Start()
    let context = server.GetContext()
    context.State |> should equal Follower
    context.CurrentTerm |> should equal 0UL
    context.PreviousLogIndexes |> should equal None

  [<Fact>]
  let ``should record operations`` () =
    fakeElectionTimeoutService.RecordedValues.Count |> should equal 0
    (fakeElectionTimeoutService :> ITimeoutService).Start()
    (fakeElectionTimeoutService :> ITimeoutService).Stop()
    fakeElectionTimeoutService.RecordedValues.Count |> should equal 2
    fakeElectionTimeoutService.RecordedValues.Item 0 |> should equal Start
    fakeElectionTimeoutService.RecordedValues.Item 1 |> should equal Stop


    

    
