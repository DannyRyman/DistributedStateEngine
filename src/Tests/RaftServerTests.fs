namespace Tests

open Xunit
open FsUnit.Xunit
open Raft
open Raft.StateTransitions
open TimerLibrary
open Tests.Logging
open Tests.Fakes

type RaftServerTests(testOutputHelper) =          
  let fakeElectionTimeoutService = new FakeTimeoutService()
  let fakeHeartbeatTimeoutService = new FakeTimeoutService()
  let nullWorkflow (initialContext:Context, _) = async {      
      return initialContext
    }
  let loggerConfig = createTestLoggerConfig testOutputHelper 
  let server = new Server(fakeElectionTimeoutService, fakeHeartbeatTimeoutService, nullWorkflow, loggerConfig)

  [<Fact>]
  let ``when server started, context must be in expected state (no previous persisted state)`` () =         
    server.Start() |> Async.RunSynchronously
    let context = server.GetContext()
    context.State |> should equal Follower
    context.CurrentTerm |> should equal 0UL
    context.PreviousLogIndexes |> should equal None

  [<Fact>]
  let ``when server started, must start the election timeout service`` () =
    server.Start() |> Async.RunSynchronously
    fakeElectionTimeoutService.RecordedValues.Count |> should equal 1
    fakeElectionTimeoutService.RecordedValues.Item 0 |> should equal Start


    


    


    

    
