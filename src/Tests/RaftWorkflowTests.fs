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

  [<Fact>]  
  let ``election timeout must start an election for followers and end in a candidate state`` () =
    let workflow = new Workflow(fakeElectionTimeout, communication, dataAccess) :> IWorkflow
    let initialContext = Follower {CurrentTerm=0UL; PreviousLogIndexes=None;}
    let newContext = workflow.ProcessRaftEvent(initialContext, ElectionTimeout)
    // Assert that the election timeout countdown is reset
    fakeElectionTimeout.RecordedValues.Single() |> should equal Reset
    // Assert the new term is persisted
    Mock.Verify(<@ dataAccess.UpdateTerm(1UL) @>, once)
    // Assert the request vote rpc call is broadcast with the expected values
    let requestVote = { Term = 1UL; CandidateId = config.ThisNode.Port.ToString(); PreviousLogIndexes = None } 
    Mock.Verify(<@ communication.Broadcast(RpcRequest (RequestVote requestVote)) @>, once)   
    // Assert transition to candidate state
    let candidateContext = 
      match newContext with 
      | Candidate c -> c
      | Follower _ | Leader _ -> failwith "Unexpected state"      
    // Assert the current term has been incremented
    candidateContext.CurrentTerm |> should equal 1UL
    // Assert the previous log indexes have not changed
    candidateContext.PreviousLogIndexes |> should equal None
    // Assert voted for self
    let onlyVote = candidateContext.CountedVotes.Single()
    onlyVote.Key |> should equal (config.ThisNode.Port.ToString())
    onlyVote.Value |> should equal true

  [<Fact>]  
  let ``election timeout must clear any previous votes`` () =
    let workflow = new Workflow(fakeElectionTimeout, communication, dataAccess) :> IWorkflow
    let existingVotes = 
      Map<string,bool> (
        [
          ("sampleVote1", true)
          ("sampleVote2", true)
        ]
      )
    let initialContext = Candidate {CurrentTerm=0UL; PreviousLogIndexes=None; CountedVotes=existingVotes }
    let newContext = workflow.ProcessRaftEvent(initialContext, ElectionTimeout)
    let candidateContext = 
      match newContext with 
      | Candidate c -> c
      | Follower _ | Leader _ -> failwith "Unexpected state"      

    // Assert that only the vote for self is registered
    let onlyVote = candidateContext.CountedVotes.Single()
    onlyVote.Key |> should equal (config.ThisNode.Port.ToString())
    onlyVote.Value |> should equal true

  // Todo start as a candidate
  // Todo clear any existing votes
  // Todo previous log entries <> none
  // Todo start term not equal to 0
  