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
    
    type Context = {
      State : State
      CurrentTerm : uint64
      PreviousLogIndexes : Option<LogIndexes>
    }
    with
      static member Init () =
        // todo load from persistence
        { 
          State = Follower
          CurrentTerm = 0UL
          PreviousLogIndexes = None
        }         
      member this.ChangeState state = 
        {this with State = state}

  module Workflow =
    open StateTransitions
    
    type RaftEvent =
      | ElectionTimeout    
        
    let processRaftEvent (initialContext:Context,raftEvent:RaftEvent) = async {
      match raftEvent with
      | ElectionTimeout -> 
        let newState = initialContext.ChangeState (Candidate)        
        return newState
    } 
  
      