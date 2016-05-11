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

let castVoteForSelf = castVote (config.ThisNode.Port.ToString())

let startElection () = 
  let currentTerm = persistedState.incrementCurrentTerm() 
  log.Information("starting election {term}", currentTerm)  
  castVoteForSelf currentTerm
    
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
      startElection()      
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
  try 
    let mailbox = new MailboxProcessor<RaftNotification>(raftState)
    mailbox.Error.Add(fun exn -> 
      log.Error("Unhandled exception in raft server {exception}. Exiting.", exn)
    )
    setupRemoteSubscriptions(mailbox)
    mailbox.Start()    
  with
    | ex -> log.Error("Unexpected exception initializing raft server {exception}. Exiting.", ex)
  cancellationToken.WaitHandle.WaitOne() |> ignore  
}