module internal RaftImplementation

open System.Threading
open CommunicationTypes
open Communication
open Logging
open fszmq
open Configuration
open TimerLibrary

module private persistedState =
  let mutable private currentTerm = 0UL  
  let mutable private votedFor : string = null 
  // todo add persistence
  let incrementCurrentTerm () =
    currentTerm <- currentTerm + 1UL
    currentTerm   
  let getCurrentTerm () =
    currentTerm   
  let getVotedFor () =
    votedFor
  let setVotedFor nodeId =
    votedFor <- nodeId
  let getLastLogIndex () =
    1UL
  let getLastLogTerm () =
    1UL
    // todo implement


let castVote candidateId term =
  let requestVote = { Term = term; CandidateId = candidateId; LastLogIndex = 1UL; LastLogTerm = 0UL }     
  broadcast (RequestVote requestVote)

let sendRequestVoteForSelf = castVote (config.ThisNode.Port.ToString())



let raftState (inbox:MailboxProcessor<RaftNotification>) =
  
  let electionTimer = new TimeoutService ((fun () -> 
    inbox.Post(ElectionTimeout)
  ), 100, 150)

  electionTimer.Start()
    
  let heartbeatTimer = new TimeoutService((fun () ->     
    // todo check previous entries
    // todo leader commit
    let emptyAppendEntry = { 
      Term = persistedState.getCurrentTerm();
      LeaderId = config.ThisNode.Port.ToString();
      PrevLogIndex = persistedState.getLastLogIndex();
      PrevLogTerm = persistedState.getLastLogTerm();
      Entries = Array.empty
      LeaderCommit = 0UL }

    log.Information("Sending heartbeat")
    broadcast (AppendEntries emptyAppendEntry)
  ), 50, 50)

  let shouldGrantVote (voteRequest:RequestVote) =    
    persistedState.getCurrentTerm() >= voteRequest.Term
    && (isNull(persistedState.getVotedFor()) || persistedState.getVotedFor() = voteRequest.CandidateId)
    && voteRequest.LastLogIndex >= persistedState.getLastLogIndex()

  let replyToVoteRequest (voteRequest:RequestVote) =
    log.Information("replying to vote request {voteRequest}", voteRequest)
    let grantVote = shouldGrantVote voteRequest
    if grantVote then
      persistedState.setVotedFor voteRequest.CandidateId
    let requestVoteResponse =  { NodeId=  config.ThisNode.Port.ToString(); Term = persistedState.getCurrentTerm(); VoteGranted = grantVote }
    unicast voteRequest.CandidateId (RequestVoteResponse requestVoteResponse)

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
        return! becomeleader()

      let! notification = receive()
      match notification with
      | ElectionTimeout -> 
        log.Information "todo - implement election timeout (candidate)"
        return! startElection ()
      | RpcCall rpcCall ->
          match rpcCall with
          | RequestVote voteRequest ->
            log.Information("received a vote request {voteRequest}", voteRequest)
            replyToVoteRequest(voteRequest)
            return! candidate(recordOfVotesCast) 
          | RequestVoteResponse voteResponse -> 
            log.Information("received a vote response {voteResponse)", voteResponse)
            let newRecordOfVotesCast = castVote recordOfVotesCast voteResponse.NodeId voteResponse.VoteGranted
            return! candidate(newRecordOfVotesCast) 
          | _ ->
            log.Information("todo - implement other responses for candidate")
            return! candidate(recordOfVotesCast) 
    }
  
  and becomeleader () =
    heartbeatTimer.Start ()
    leader()

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

    mailbox.Error.Add(fun exn -> 
      log.Error("Unhandled exception in raft server {exception}. Exiting.", exn)
    )
    setupRemoteSubscriptions(mailbox)
    mailbox.Start()    
  with
    | ex -> log.Error("Unexpected exception initializing raft server {exception}. Exiting.", ex)
  cancellationToken.WaitHandle.WaitOne() |> ignore  
}