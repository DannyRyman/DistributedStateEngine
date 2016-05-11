module internal RaftImplementation

open System.Threading
open CommunicationTypes
open Communication
open Logging
open fszmq

module private persistedState =
  let mutable private currentTerm = 0UL  
  // todo add persistence
  let incrementCurrentTerm () =
    currentTerm <- currentTerm + 1UL
    currentTerm   
  let getCurrentTerm () =
    currentTerm   

let castVote term =
  let requestVote = { Term = term; CandidateId = "Todo"; LastLogIndex = 1UL; LastLogTerm = 0UL }     
  broadcast (RequestVote requestVote)

let startElection () = 
  let currentTerm = persistedState.incrementCurrentTerm() 
  log.Information("starting election {term}", currentTerm)  
  try
    castVote currentTerm
  with
    | :? ZMQError as ex -> printfn "Zeromq Error %i %s" ex.ErrorNumber (ex.ToString())
    | ex -> printfn "something bad happened %s" (ex.ToString())
    
let raftState (inbox:MailboxProcessor<RaftNotification>) =
    
  let receiveOrTimeout() = 
    async { 
      // todo randomise the timeout
      let! result = inbox.Receive(100) |> Async.Catch
      return match result with
              | Choice1Of2 r -> r
              | Choice2Of2 _ -> ElectionTimeout
    }
  
  let rec follower() = 
    async {          
      let! notification = receiveOrTimeout()
      match notification with
      | ElectionTimeout -> 
        log.Information "Election timeout received. Transitioning from follower -> candidate"
        return! candidate()
      | RpcCall _ -> log.Information "todo - implement rpc in (follower)"
      return! follower()
    }
  
  and candidate() = 
    async {
      startElection ()
      let! notification = receiveOrTimeout()
      match notification with
      | ElectionTimeout -> 
        log.Information "todo - implement election timeout (candidate)"
        return! candidate()
      | RpcCall _ -> log.Information "todo - implement rpc in (candidate)"
      return! candidate()
    }
 
  follower()

let init (cancellationToken : CancellationToken) = async {    
  let mailbox = new MailboxProcessor<RaftNotification>(raftState)
  setupRemoteSubscriptions(mailbox)
  mailbox.Start()    
  cancellationToken.WaitHandle.WaitOne() |> ignore  
}