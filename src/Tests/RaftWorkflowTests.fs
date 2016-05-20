namespace Tests

open Logging
open Xunit
open Raft
open FsUnit.Xunit
open Fakes
open System.Linq
open TimerLibrary

type RaftWorkflowTests(testOutputHelper) =
  let loggerConfig = createTestLoggerConfig testOutputHelper 
  let fakeElectionTimeout = new FakeTimeoutService()
  let workflow = new Workflow(fakeElectionTimeout) :> IWorkflow

  (*
  [<Theory>]
  [<MemberData("FollowerAndCandidateState")>]
  let ``election timeout must start an election for followers and candidates and end in a candidate state`` (initialState:State) =
    let initialContext = {State=initialState; CurrentTerm=0UL; PreviousLogIndexes=None}
    let newContext = workflow.ProcessRaftEvent(initialContext, ElectionTimeout)
    fakeElectionTimeout.RecordedValues.Single() |> should equal Reset
    
    failwith "test"

  
  let startElection () =
      let currentTerm = persistedState.incrementCurrentTerm() 
      log.Information("starting election {term}", currentTerm)        
      electionTimeout.Reset ()     
      broadcastRequestVote ()
      let updatedContext = {
        State = Candidate
        CountedVotes = Map<string,bool>([(config.ThisNode.Port.ToString(), true)]) 
      }
      updatedContext
  *)
  
  static member FollowerAndCandidateState : obj array seq = 
    seq {
      yield [|Follower|]
      yield [|Candidate|]
    }