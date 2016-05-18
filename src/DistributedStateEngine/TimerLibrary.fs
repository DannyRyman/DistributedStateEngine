module TimerLibrary
open System

type TimeoutServiceOperations =
  | Reset
  | Start
  | Stop

type TimerParams = 
 {
   MinimumTimeout : int
   MaximumTimeout : int
 }

type ITimeoutService = 
  abstract member Start : unit -> unit
  abstract member Stop : unit -> unit
  abstract member Reset : unit -> unit
  abstract member TimedOut : IObservable<unit>

type TimeoutService(timerParams) =  

  let evt = Event<_>()

  let mailbox = MailboxProcessor<TimeoutServiceOperations>.Start(fun agent ->
    let rec started () = async {
        let rnd = System.Random()
        let timeout = rnd.Next(timerParams.MinimumTimeout,timerParams.MaximumTimeout)
        let! r = agent.TryReceive(timeout)      
        match r with
        | Some operation -> 
          match operation with
          | Reset -> return! started()
          | Start -> return! started()
          | Stop -> return! stopped()
        | None -> evt.Trigger(); return! started ()       
      }
    and stopped () = async {
      let! r = agent.Receive()      
      match r with
      | Reset -> return! stopped()
      | Start -> return! started()
      | Stop -> return! stopped()      
    } 
    stopped ()
  )
  
  interface ITimeoutService with    
    member x.Start() = mailbox.Post(Start)
    member x.Stop() = mailbox.Post(Stop)
    member x.Reset() = mailbox.Post(Reset)
    member x.TimedOut = evt.Publish :> _

//
//let f() = printfn "Hello"
//
//let evt = Event<_>()
//let timer = TimeoutService(evt, 100, 100)
//
//let sub = evt.Publish.Subscribe f
//timer.Start()