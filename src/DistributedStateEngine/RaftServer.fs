namespace Raft
  open Raft.StateTransitions
  open TimerLibrary
  open System
  open Raft.Workflow
  open Logging
  open Serilog

  type Server(electionTimeoutService : ITimeoutService, 
              heartbeatTimeoutService : ITimeoutService,
              raftWorkflow:(Context*RaftEvent)->Context,
              loggerConfig:LoggerConfiguration) =         
    do
      Log.Logger <- loggerConfig.CreateLogger()

    let mutable context:Option<Context> = None

    let workflowProcessor = MailboxProcessor<RaftEvent>.Start(fun mailbox -> 
      async {
        let! event = mailbox.Receive()
        Log.Information("Received event {event}", event)        
        let newContext = raftWorkflow (context.Value, event)
        Log.Information("Finished processing {event}; Original context {context}; New Context {newContext}", sprintf "%A" event, sprintf "%A" context.Value, sprintf "%A" newContext)
        context <- Some newContext
      }      
    )

    let initializeContext () =
      // todo load from persistance if necessary
      Some {State=Follower;CurrentTerm=0UL;PreviousLogIndexes=None}

    member this.Start () = 
      async {
        context <- initializeContext ()
        workflowProcessor.Error.Add(fun ex -> Log.Error("Error {ex}", ex))
        electionTimeoutService.TimedOut.Subscribe(fun () -> workflowProcessor.Post(ElectionTimeout)) |> ignore<IDisposable>      
        electionTimeoutService.Start()      
      }        

    member this.GetContext () =
      match context with
      | Some c -> c
      | None -> failwith "Cannot get context as server is not started"    
      
        
