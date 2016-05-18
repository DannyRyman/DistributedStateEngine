namespace Raft
  open Raft.StateTransitions
  open TimerLibrary
  open System

  type Server(electionTimeoutService : ITimeoutService, heartbeatTimeoutService : ITimeoutService) =     
    
    let mutable context:Option<Context> = None

    member this.Start () =        
      context <- Some <| Context.Init()
      electionTimeoutService.TimedOut.Subscribe(fun () -> printfn "Timed out") |> ignore<IDisposable>
      electionTimeoutService.Start()      
      heartbeatTimeoutService.TimedOut.Subscribe(fun () -> printfn "Heartbeat timed out") |> ignore<IDisposable>
      heartbeatTimeoutService.Start()
      printf "test"

    member this.GetContext () =
      match context with
      | Some c -> c
      | None -> failwith "Cannot get context as server is not started"    
      
        
