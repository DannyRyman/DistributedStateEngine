module Raft
open TimerLibrary
open Serilog
open System
open System.Threading
open Configuration

//---------------------------------------------------------------------------
// Types
//---------------------------------------------------------------------------
type LogIndexes = {
  Index : uint64
  Term : uint64
}

type FollowerContext = {
  CurrentTerm : uint64
  PreviousLogIndexes : Option<LogIndexes>
}

type CandidateContext = {
  CurrentTerm : uint64
  PreviousLogIndexes : Option<LogIndexes>
  CountedVotes : Map<string,bool>
} 

type State = 
  | Follower of FollowerContext
  | Candidate of CandidateContext
  | Leader    

type RaftEvent =
  | ElectionTimeout    

type ServerEvents =
  | ServerStarted

type AppendEntries = 
  { Term : uint64
    LeaderId : string
    PreviousLogIndexes: Option<LogIndexes>
    Entries : string []
    LeaderCommit : uint64 }

type AppendEntriesResponse = 
  { Term : uint64
    Success : bool }

type RequestVote = 
  { Term : uint64
    CandidateId : string
    PreviousLogIndexes: Option<LogIndexes> }

type RequestVoteResponse = 
  { NodeId: string
    Term : uint64
    VoteGranted : bool }

type RpcRequest =   
  | RequestVote of RequestVote  
  | AppendEntries of AppendEntries  

type RpcResponse =
  | RequestVoteResponse of RequestVoteResponse  
  | AppendEntriesResponse of AppendEntriesResponse

type RpcCall =
  | RpcRequest of RpcRequest
  | RpcResponse of RpcResponse 
        
//---------------------------------------------------------------------------
// Communication
//---------------------------------------------------------------------------
type ICommunication =  
  abstract member Unicast : destination:string * rpcCall:RpcCall -> unit
  abstract member Broadcast : rpcCall:RpcCall -> unit
  
type Communication() =
  interface ICommunication with
    member x.Unicast (destination:string, rpcCall:RpcCall) =
      printfn "todo implement unicast messaging"
    member x.Broadcast (rpcCall:RpcCall) = 
      printfn "todo implement broadcast messaging"

//---------------------------------------------------------------------------
// Data Access 
//---------------------------------------------------------------------------
type IDataAccess =
  abstract member UpdateTerm : UInt64 -> unit 

type DataAccess() = 
  interface IDataAccess with
    member x.UpdateTerm term =
      printfn "todo persist the term"

//---------------------------------------------------------------------------
// Main raft workflow
//---------------------------------------------------------------------------
type IWorkflow = 
  abstract member ProcessRaftEvent : (State*RaftEvent)->State

type Workflow(electionTimeoutService : ITimeoutService,
              communication: ICommunication,
              dataAccess: IDataAccess) =

  let startElection (initialState:State) (initialTerm:UInt64) =
    electionTimeoutService.Reset()
    let newTerm = initialTerm + 1UL    
    dataAccess.UpdateTerm(newTerm)
    // todo remove hard coded previous log entry
    let requestVote = { Term = newTerm; CandidateId = config.ThisNode.Port.ToString(); PreviousLogIndexes = None } 
    communication.Broadcast(RpcRequest (RequestVote requestVote))
    Candidate {
      CurrentTerm=newTerm
      PreviousLogIndexes=None
      CountedVotes=Map<string,bool>([(config.ThisNode.Port.ToString(), true)])  
    }

  let processElectionTimeout (initialState:State) =
    match initialState with
    | Follower f -> startElection (initialState) (f.CurrentTerm)
    | Candidate c -> startElection (initialState) (c.CurrentTerm)
    | Leader -> failwith "election timeout should not be raised when in the leader state"  

  interface IWorkflow with
    member this.ProcessRaftEvent x = 
      let initialContext, raftEvent = x
      match raftEvent with
      | ElectionTimeout -> processElectionTimeout <| initialContext    
    
//---------------------------------------------------------------------------
// Raft server
//---------------------------------------------------------------------------
type Server(electionTimeoutService : ITimeoutService, 
              heartbeatTimeoutService : ITimeoutService,
              raftWorkflow:IWorkflow,
              loggerConfig:LoggerConfiguration) =         
  do
    Log.Logger <- loggerConfig.CreateLogger()

  let serverEvent = new Event<ServerEvents>()

  let mutable state:Option<State> = None

  let workflowProcessor = MailboxProcessor<RaftEvent>.Start(fun mailbox -> 
    async {
      let! event = mailbox.Receive()
      Log.Information("Received event {event}", event)        
      let newState = raftWorkflow.ProcessRaftEvent (state.Value, event)
      Log.Information("Finished processing {event}; Original context {context}; New Context {newContext}", sprintf "%A" event, sprintf "%A" state.Value, sprintf "%A" newState)
      state <- Some newState
    }      
  )

  let initializeState () =
    // todo load from persistance if necessary
    Some (Follower {
      CurrentTerm=0UL
      PreviousLogIndexes=None}
      )

  member this.ServerEvent = serverEvent.Publish

  member this.Start (cancellationToken : CancellationToken) = 
    async {
      state <- initializeState ()
      workflowProcessor.Error.Add(fun ex -> Log.Error("Error {ex}", ex))
      use electionTimeoutServiceSubscription = electionTimeoutService.TimedOut.Subscribe(fun () -> workflowProcessor.Post(ElectionTimeout))      
      electionTimeoutService.Start()
      serverEvent.Trigger(ServerStarted)
      cancellationToken.WaitHandle.WaitOne() |> ignore    
    }        

  member this.GetState () =
    match state with
    | Some c -> c
    | None -> failwith "Cannot get context as server is not started"    
      
        

  
      