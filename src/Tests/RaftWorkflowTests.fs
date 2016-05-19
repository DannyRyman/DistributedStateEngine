namespace Tests

open Logging
open Xunit
open Raft.Workflow
open Raft.StateTransitions
open FsUnit.Xunit

type RaftWorkflowTests(testOutputHelper) =
  let loggerConfig = createTestLoggerConfig testOutputHelper 

  [<Fact>]
  let ``election timeout must transition followers to candidates`` () =
    let initialContext = {State=Follower; CurrentTerm=0UL; PreviousLogIndexes=None}
    let newContext = processRaftEvent (initialContext, ElectionTimeout)
    newContext.State |> should equal Candidate

