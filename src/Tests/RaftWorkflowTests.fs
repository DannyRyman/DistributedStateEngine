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

type WhenRaftElectionTimeoutOccurs(testOutputHelper) =
  let loggerConfig = createTestLoggerConfig testOutputHelper 
  let fakeElectionTimeout = new FakeTimeoutService()
  let communication = Mock.Of<ICommunication>()
  let dataAccess = Mock.Of<IDataAccess>()

  let doElectionTimeoutAndReturnCandidateState initialState =
    let workflow = new Workflow(fakeElectionTimeout, communication, dataAccess) :> IWorkflow    
    let newState = workflow.ProcessRaftEvent(initialState, ElectionTimeout)
    let candidateState = 
      match newState with 
      | Candidate c -> c
      | Follower _ | Leader _ -> failwith "Unexpected state"
    candidateState

  [<Fact>]  
  let ``must start an election for followers and end in a candidate state`` () =       
    let newState = doElectionTimeoutAndReturnCandidateState(Follower {CurrentTerm=0UL; PreviousLogIndexes=None;})

    // Assert that the election timeout countdown is reset
    fakeElectionTimeout.RecordedValues.Single() |> should equal Reset
    // Assert the new term is persisted
    Mock.Verify(<@ dataAccess.UpdateTerm(1UL) @>, once)
    // Assert the request vote rpc call is broadcast with the expected values
    let requestVote = { Term = 1UL; CandidateId = config.ThisNode.Port.ToString(); PreviousLogIndexes = None } 
    Mock.Verify(<@ communication.Broadcast(RpcRequest (RequestVote requestVote)) @>, once)    
    // Assert the current term has been incremented
    newState.CurrentTerm |> should equal 1UL
    // Assert the previous log indexes have not changed
    newState.PreviousLogIndexes |> should equal None
    // Assert voted for self
    let onlyVote = newState.CountedVotes.Single()
    onlyVote.Key |> should equal (config.ThisNode.Port.ToString())
    onlyVote.Value |> should equal true

  [<Fact>]  
  let ``must clear any previous votes`` () =
    
    let existingVotes = 
      Map<string,bool> (
        [
          ("sampleVote1", true)
          ("sampleVote2", true)
        ]
      )
    let initialContext = Candidate {CurrentTerm=0UL; PreviousLogIndexes=None; CountedVotes=existingVotes }
    let newState = doElectionTimeoutAndReturnCandidateState (initialContext)
    // Assert that only the vote for self is registered
    let onlyVote = newState.CountedVotes.Single()
    onlyVote.Key |> should equal (config.ThisNode.Port.ToString())
    onlyVote.Value |> should equal true

  [<Fact>]
  let ``example where the term is greater than zero and the log indexes are set`` () =    
    let previousLogIndexes = Some {Term=10UL;Index=5UL}
    let newState = doElectionTimeoutAndReturnCandidateState (Follower {CurrentTerm=11UL; PreviousLogIndexes=previousLogIndexes; })
    // The previous log indexes should be sent with the request vote    
    let requestVote = { Term = 12UL; CandidateId = config.ThisNode.Port.ToString(); PreviousLogIndexes = previousLogIndexes } 
    Mock.Verify(<@ communication.Broadcast(RpcRequest (RequestVote requestVote)) @>, once)   
    // The previous log indexes must remain unchanged
    newState.PreviousLogIndexes.IsSome |> should equal true
    newState.PreviousLogIndexes.Value.Index |> should equal 5UL
    newState.PreviousLogIndexes.Value.Term |> should equal 10UL
    newState.CurrentTerm |> should equal 12UL
  