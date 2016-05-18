namespace Raft

  module StateTransitions =
    
    type State = 
      | Follower
      | Candidate
      | Leader

    type LogIndexes = {
      Index : uint64
      Term : uint64
    }
    
    type Context = private {
      state : State
      currentTerm : uint64
      previousLogIndexes : Option<LogIndexes>
    }
    with
      static member Init () =
        // todo load from persistence
        { 
          state = Follower
          currentTerm = 0UL
          previousLogIndexes = None
        }   

      member this.State = this.state
      member this.CurrentTerm = this.currentTerm
      member this.PreviousLogIndexes = this.previousLogIndexes   

  module Workflow =
    open StateTransitions
            
    let processRaftEvent initialContext raftEvent = async {
      printf "todo - implement"
      initialContext
    } 
  
      