module Raft
open TimerLibrary
open Serilog
open System
open System.Threading
open Configuration

//---------------------------------------------------------------------------
// Types
//---------------------------------------------------------------------------

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
  // Todo this only relates to the candidate state. Remodel to make this unrepresentable in the other states.
  CountedVotes : Map<string,bool>
}

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
  abstract member ProcessRaftEvent : (Context*RaftEvent)->Context

type Workflow(electionTimeoutService : ITimeoutService,
              communication: ICommunication,
              dataAccess: IDataAccess) =

  let startElection (initialContext:Context) =
    electionTimeoutService.Reset()
    let newTerm = initialContext.CurrentTerm + 1UL    
    dataAccess.UpdateTerm(newTerm)
    // todo remove hard coded previous log entry
    let requestVote = { Term = newTerm; CandidateId = config.ThisNode.Port.ToString(); PreviousLogIndexes = None } 
    communication.Broadcast(RpcRequest (RequestVote requestVote))
    {initialContext with 
      State=Candidate 
      CurrentTerm=newTerm
      CountedVotes=Map<string,bool>([(config.ThisNode.Port.ToString(), true)]) 
      }

  let processElectionTimeout (initialContext:Context) =
    match initialContext.State with
    | Follower | Candidate -> startElection <| initialContext
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

  let mutable context:Option<Context> = None

  let workflowProcessor = MailboxProcessor<RaftEvent>.Start(fun mailbox -> 
    async {
      let! event = mailbox.Receive()
      Log.Information("Received event {event}", event)        
      let newContext = raftWorkflow.ProcessRaftEvent (context.Value, event)
      Log.Information("Finished processing {event}; Original context {context}; New Context {newContext}", sprintf "%A" event, sprintf "%A" context.Value, sprintf "%A" newContext)
      context <- Some newContext
    }      
  )

  let initializeContext () =
    // todo load from persistance if necessary
    Some {State=Follower;CurrentTerm=0UL;PreviousLogIndexes=None;CountedVotes=Map.empty}

  member this.ServerEvent = serverEvent.Publish

  member this.Start (cancellationToken : CancellationToken) = 
    async {
      context <- initializeContext ()
      workflowProcessor.Error.Add(fun ex -> Log.Error("Error {ex}", ex))
      use electionTimeoutServiceSubscription = electionTimeoutService.TimedOut.Subscribe(fun () -> workflowProcessor.Post(ElectionTimeout))      
      electionTimeoutService.Start()
      serverEvent.Trigger(ServerStarted)
      cancellationToken.WaitHandle.WaitOne() |> ignore    
    }        

  member this.GetContext () =
    match context with
    | Some c -> c
    | None -> failwith "Cannot get context as server is not started"    
      
        

  
      