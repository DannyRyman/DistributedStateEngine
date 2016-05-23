namespace Tests

open Logging
open Xunit
open Raft
open FsUnit.Xunit
open Fakes
open System.Linq
open TimerLibrary
open Configuration
open Foq

type RaftWorkflowTests(testOutputHelper) =
  let loggerConfig = createTestLoggerConfig testOutputHelper 
  let fakeElectionTimeout = new FakeTimeoutService()
  let communication = Mock.Of<ICommunication>()
  let dataAccess = Mock.Of<IDataAccess>()

  [<Theory>]
  [<MemberData("FollowerAndCandidateState")>]
  let ``election timeout must start an election for followers and candidates and end in a candidate state`` (initialState:State) =
    let workflow = new Workflow(fakeElectionTimeout, communication, dataAccess) :> IWorkflow
    let initialContext = {State=initialState; CurrentTerm=0UL; PreviousLogIndexes=None; CountedVotes = Map.empty}
    let newContext = workflow.ProcessRaftEvent(initialContext, ElectionTimeout)
    // Assert that the election timeout countdown is reset
    fakeElectionTimeout.RecordedValues.Single() |> should equal Reset
    // Assert the new term is persisted
    Mock.Verify(<@ dataAccess.UpdateTerm(1UL) @>, once)
    // Assert the request vote rpc call is broadcast with the expected values
    let requestVote = { Term = 1UL; CandidateId = config.ThisNode.Port.ToString(); PreviousLogIndexes = None } 
    Mock.Verify(<@ communication.Broadcast(RpcRequest (RequestVote requestVote)) @>, once)   
    // Assert transition to candidate state
    newContext.State |> should equal Candidate
    // Assert the current term has been incremented
    newContext.CurrentTerm |> should equal 1UL
    // Assert the previous log indexes have not changed
    newContext.PreviousLogIndexes |> should equal None
    // Assert voted for self
    let onlyVote = newContext.CountedVotes.Single()
    onlyVote.Key |> should equal (config.ThisNode.Port.ToString())
    onlyVote.Value |> should equal true
  
  // Todo clear any existing votes
  // Todo previous log entries <> none
  // Todo start term not equal to 0
  
  static member FollowerAndCandidateState : obj array seq = 
    seq {
      yield [|Follower|]
      yield [|Candidate|]
    }