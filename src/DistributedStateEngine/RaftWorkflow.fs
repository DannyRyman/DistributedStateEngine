module Raft
open TimerLibrary
open Serilog
open System
open System.Threading

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
}

type RaftEvent =
  | ElectionTimeout    

type ServerEvents =
  | ServerStarted
        
//---------------------------------------------------------------------------
// Main raft workflow
//---------------------------------------------------------------------------
type IWorkflow = 
  abstract member ProcessRaftEvent : (Context*RaftEvent)->Context

type Workflow(electionTimeoutService : ITimeoutService) =
  let startElection (initialContext:Context) =
    electionTimeoutService.Reset()
    initialContext  

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
    Some {State=Follower;CurrentTerm=0UL;PreviousLogIndexes=None}

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
      
        

  
      