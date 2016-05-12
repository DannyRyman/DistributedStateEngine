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
  
  let castVote (recordOfVotesCast:Map<string, bool>) (nodeId:string) (isYesVote:bool) =        
    recordOfVotesCast.Add(nodeId, isYesVote)

  let rec follower() = 
    async {
      let! notification = receive ()
      match notification with
      | ElectionTimeout -> 
        log.Information "Election timeout received. Transitioning from follower -> candidate"
        return! startElection ()
      | RpcCall _ -> log.Information "todo - implement rpc in (follower)"
      return! follower ()
    }
 
  and startElection () =
    let currentTerm = persistedState.incrementCurrentTerm() 
    log.Information("starting election {term}", currentTerm)  
    let recordOfVotesCast = castVote Map.empty (config.ThisNode.Port.ToString()) true
    electionTimer.Reset ()
    sendRequestVoteForSelf currentTerm
    candidate (recordOfVotesCast)

  and candidate (recordOfVotesCast:Map<string, bool>) = 
    async {
      let n = config.OtherNodes.Count
      let majority = n / 2 + 1 // int division
      
      // todo only count voteGranted=true responses
      if recordOfVotesCast.Count > majority then
        printfn "todo implement becoming a leader"
        return! leader()

      let! notification = receive()
      match notification with
      | ElectionTimeout -> 
        log.Information "todo - implement election timeout (candidate)"
        return! startElection ()
      | RpcCall rpcCall ->
          match rpcCall with
          | RequestVoteResponse voteResponse -> 
            log.Information("received a vote response {voteResponse)", voteResponse)
            let newRecordOfVotesCast = castVote recordOfVotesCast voteResponse.NodeId voteResponse.VoteGranted
            return! candidate(newRecordOfVotesCast) 
          | _ ->
            log.Information("todo - implement other responses for candidate")
            return! candidate(recordOfVotesCast) 
    }
   
  and leader () = 
    async {
      let! notification = receive()
      match notification with
      | ElectionTimeout -> 
        log.Information "todo - implement election timeout (leader)"
        return! leader ()
      | RpcCall _ -> 
          log.Information "todo - implement rpc in (leader)"
          return! leader ()
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