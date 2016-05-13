module internal RaftImplementation

open System.Threading
open CommunicationTypes
open Communication
open Logging
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
    // todo implement
    1UL    

type State = 
  | Follower
  | Candidate
  | Leader

// todo is the a generic way to do this?
let getStateName state = 
  match state with 
  | Follower -> "Follower"
  | Candidate -> "Candidate"
  | Leader -> "Leader"

let getMessageName (message:RaftNotification) =
  match message with
  | ElectionTimeout -> "ElectionTimeout"
  | RpcCall rpc ->
    match rpc with 
    | RpcRequest (RequestVote _) -> "RequestVote"
    | RpcResponse (RequestVoteResponse _) -> "RequestVoteResponse"     
    | RpcRequest (AppendEntries _) -> "AppendEntries"
    | RpcResponse (AppendEntriesResponse _) -> "AppendEntriesResponse"

type Context = {
  State : State
  CountedVotes : Map<string, bool>
}

type Server() = 

  let initialContext = {
    State = Follower
    CountedVotes = Map.empty
  }

  let notificationHandler (inbox:MailboxProcessor<RaftNotification>) =

    let electionTimeout = new TimeoutService ((fun () -> 
      printfn "election timeout"
      inbox.Post(ElectionTimeout)
    ), 100, 150) 

    let createAppendEntries (entries) =
      { Term = persistedState.getCurrentTerm()
        LeaderId = "todo" // todo
        PrevLogIndex = persistedState.getLastLogIndex()
        PrevLogTerm = persistedState.getLastLogTerm()
        Entries = entries
        LeaderCommit = 1UL } // todo}

    let heartbeatTimeout = new TimeoutService((fun () -> 
      printfn "sending heartbeat"
      let appendEntries = createAppendEntries [||]
      broadcast (RpcRequest (AppendEntries appendEntries))
    ), 50, 50)

    let intialize () = 
      electionTimeout.Start()
      initialContext

    let broadcastRequestVote () = 
      let requestVote = { Term = persistedState.getCurrentTerm(); CandidateId = config.ThisNode.Port.ToString(); LastLogIndex = 1UL; LastLogTerm = 0UL }     
      broadcast (RpcRequest (RequestVote requestVote))

    let startElection () =
      let currentTerm = persistedState.incrementCurrentTerm() 
      log.Information("starting election {term}", currentTerm)        
      electionTimeout.Reset ()     
      broadcastRequestVote ()
      let updatedContext = {
        State = Candidate
        CountedVotes = Map<string,bool>([(config.ThisNode.Port.ToString(), true)]) 
      }
      updatedContext

    let shouldGrantVote (voteRequest:RequestVote) =         
      persistedState.getCurrentTerm() >= voteRequest.Term
      && (isNull(persistedState.getVotedFor()) || persistedState.getVotedFor() = voteRequest.CandidateId)
      && voteRequest.LastLogIndex >= persistedState.getLastLogIndex()
    
    let replyToVoteRequest (context:Context) voteRequest =      
      let grantVote = shouldGrantVote voteRequest
      let requestVoteResponse =  { NodeId=  config.ThisNode.Port.ToString(); Term = persistedState.getCurrentTerm(); VoteGranted = grantVote }
      unicast voteRequest.CandidateId (RpcResponse (RequestVoteResponse requestVoteResponse))
      context
    
    let becomeLeader () =
      electionTimeout.Stop()
      heartbeatTimeout.Start()
      let leaderState = { 
        State = Leader
        CountedVotes = Map.empty
      }
      leaderState
    
    let processVote (context:Context) (voteResponse:RequestVoteResponse) =  
      let n = config.OtherNodes.Count
      let majority = n / 2 + 1 // int division
      let updatedCountedVotes = context.CountedVotes.Add(voteResponse.NodeId, voteResponse.VoteGranted)  

      if updatedCountedVotes.Count >= majority then
        becomeLeader ()
      else        
        {context with         
            CountedVotes = updatedCountedVotes}
     
    let demoteToFollowerIfTermExceeded (initialContext:Context) (inbox:MailboxProcessor<RaftNotification>) (request : RpcRequest) =
      let term = match request with
                  | AppendEntries r -> r.Term
                  | RequestVote r -> r.Term                                    

      let shouldDemote = term > persistedState.getCurrentTerm()
      match shouldDemote with
      | true ->         
        // repost the message so that it can be rehandled in the follower state
        inbox.Post (RpcCall (RpcRequest request))
        let newContext = { 
          State = Follower
          CountedVotes = Map.empty
          }
        newContext
      | false -> initialContext   

    let rec listenForMessages (initialContext:Context) = async {      

      // partial apply local functions to make them easier to work with
      let demoteToFollowerIfTermExceeded = demoteToFollowerIfTermExceeded initialContext inbox
      let replyToVoteRequest = replyToVoteRequest initialContext
      let processVote = processVote initialContext
      let doNothing () = initialContext

      let! msg = inbox.Receive()                  
      log.Information("received message:{msg} current state: {state}", getMessageName(msg), getStateName(initialContext.State))
      
      // Process any messages returning the updated context
      let newContext = 
        match msg with 
        | ElectionTimeout ->
          match initialContext.State with
          | Follower | Candidate -> startElection ()
          | Leader -> doNothing ()
        | RpcCall (RpcRequest (RequestVote r)) ->
          match initialContext.State with
          | Follower | Candidate -> replyToVoteRequest r
          | Leader -> demoteToFollowerIfTermExceeded (RequestVote r) 
        | RpcCall (RpcResponse (RequestVoteResponse r)) ->
          match initialContext.State with
          | Candidate -> processVote r
          | Follower | Leader -> doNothing ()
        | _ -> doNothing ()

      return! listenForMessages newContext
    }

    listenForMessages(intialize ())       
   
  member x.Start (cancellationToken : CancellationToken) = 
    let mailbox = new MailboxProcessor<RaftNotification>(notificationHandler)
    setupRemoteSubscriptions(mailbox)
    mailbox.Start()        
    cancellationToken.WaitHandle.WaitOne() |> ignore 