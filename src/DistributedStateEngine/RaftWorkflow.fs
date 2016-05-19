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

  module Workflow =
    open StateTransitions
    
    type RaftEvent =
      | ElectionTimeout    
        
    let processRaftEvent (initialContext:Context,raftEvent:RaftEvent) = 
      match raftEvent with
      | ElectionTimeout -> 
        { initialContext with State = Follower }
    
  
      