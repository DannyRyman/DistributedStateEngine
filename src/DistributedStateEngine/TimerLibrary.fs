module TimerLibrary

type TimeoutServiceOperations =
  | Reset
  | Start
  | Stop

type TimeoutService(fn, minimumTimeout, maximumTimeout) =  

  let timeout fn = MailboxProcessor<TimeoutServiceOperations>.Start(fun agent ->
    let rec started () = async {
        let rnd = System.Random()
        let timeout = rnd.Next(minimumTimeout,maximumTimeout)
        let! r = agent.TryReceive(timeout)      
        match r with
        | Some operation -> 
          match operation with
          | Reset -> return! started()
          | Start -> return! started()
          | Stop -> return! stopped()
        | None -> fn(); return! started ()       
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
  let mailbox = timeout fn
  member x.Start() = mailbox.Post(Start)
  member x.Stop() = mailbox.Post(Stop)
  member x.Reset() = mailbox.Post(Reset)