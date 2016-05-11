module internal RaftImplementation

open System.Threading
open CommunicationTypes
open Communication
open Logging
open fszmq
open Configuration

module private persistedState =
  let mutable private currentTerm = 0UL  
  // todo add persistence
  let incrementCurrentTerm () =
    currentTerm <- currentTerm + 1UL
    currentTerm   
  let getCurrentTerm () =
    currentTerm   

let castVote candidateId term =
  let requestVote = { Term = term; CandidateId = candidateId; LastLogIndex = 1UL; LastLogTerm = 0UL }     
  broadcast (RequestVote requestVote)

let sendRequestVoteForSelf = castVote (config.ThisNode.Port.ToString())

type TimeoutService(fn) =
  let timeout fn = MailboxProcessor.Start(fun agent ->
    let rec loop () = async {
      let rnd = System.Random()
      let timeout = rnd.Next(100,150)
      let! r = agent.TryReceive(timeout)      
      match r with
      | Some _ -> return! loop ()
      | None -> fn(); return! loop ()       
    }
    loop ()
  )
  let mailbox = timeout fn
  member x.Reset() = mailbox.Post(null)

let raftState (inbox:MailboxProcessor<RaftNotification>) =
  
  let electionTimer = new TimeoutService(fun () -> 
    inbox.Post(ElectionTimeout)
  )
    
  let receive() = 
    async { 
      // todo randomise the timeout
      let! result = inbox.Receive() |> Async.Catch
      return match result with
              | Choice1Of2 r -> r
              | Choice2Of2 _ -> ElectionTimeout
    }
  
  let rec follower() = 
    async {
      let! notification = receive()
      match notification with
      | ElectionTimeout -> 
        log.Information "Election timeout received. Transitioning from follower -> candidate"
        return! candidate()
      | RpcCall _ -> log.Information "todo - implement rpc in (follower)"
      return! follower()
    }
  
  and candidate() = 
    async {
      let currentTerm = persistedState.incrementCurrentTerm() 
      log.Information("starting election {term}", currentTerm)  
      // todo increment vote count
      electionTimer.Reset ()
      sendRequestVoteForSelf currentTerm      

      let! notification = receive()
      match notification with
      | ElectionTimeout -> 
        log.Information "todo - implement election timeout (candidate)"
        return! candidate()
      | RpcCall _ -> log.Information "todo - implement rpc in (candidate)"
      return! candidate()
    }
 
  follower()



let init (cancellationToken : CancellationToken) = async {     
  try 
    
    let mailbox = new MailboxProcessor<RaftNotification>(raftState)

    let timeoutService = new TimeoutService(fun () -> 
      mailbox.Post ElectionTimeout
    )

    mailbox.Error.Add(fun exn -> 
      log.Error("Unhandled exception in raft server {exception}. Exiting.", exn)
    )
    setupRemoteSubscriptions(mailbox)
    mailbox.Start()    
  with
    | ex -> log.Error("Unexpected exception initializing raft server {exception}. Exiting.", ex)
  cancellationToken.WaitHandle.WaitOne() |> ignore  
}