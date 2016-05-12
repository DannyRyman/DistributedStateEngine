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
  | RpcCall (RequestVote _) -> "RequestVote"
  | RpcCall (RequestVoteResponse _) -> "RequestVoteResponse"
  | RpcCall (AppendEntries _) -> "AppendEntries"
  | RpcCall (AppendEntriesResponse _) -> "AppendEntriesResponse"

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

    electionTimeout.Start()

    let broadcastRequestVote () = 
      let requestVote = { Term = persistedState.getCurrentTerm(); CandidateId = config.ThisNode.Port.ToString(); LastLogIndex = 1UL; LastLogTerm = 0UL }     
      broadcast (RequestVote requestVote)

    let startElection context =
      let currentTerm = persistedState.incrementCurrentTerm() 
      log.Information("starting election {term}", currentTerm)        
      electionTimeout.Reset ()
      broadcastRequestVote ()
      {context with 
        State = Candidate
        CountedVotes = context.CountedVotes.Add(config.ThisNode.Port.ToString(), true)}

    let shouldGrantVote (voteRequest:RequestVote) =         
      persistedState.getCurrentTerm() >= voteRequest.Term
      && (isNull(persistedState.getVotedFor()) || persistedState.getVotedFor() = voteRequest.CandidateId)
      && voteRequest.LastLogIndex >= persistedState.getLastLogIndex()
    
    let replyToVoteRequest voteRequest =      
      let grantVote = shouldGrantVote voteRequest
      let requestVoteResponse =  { NodeId=  config.ThisNode.Port.ToString(); Term = persistedState.getCurrentTerm(); VoteGranted = grantVote }
      unicast voteRequest.CandidateId (RequestVoteResponse requestVoteResponse) 
    
    let becomeLeader () =
      electionTimeout.Stop()
      let leaderState = { 
        State = Leader
        CountedVotes = Map.empty
      }
      leaderState
      
    let receiveVoteResponse (context:Context) (voteResponse:RequestVoteResponse) =  
      let n = config.OtherNodes.Count
      let majority = n / 2 + 1 // int division
      // todo what should be done with the term info?
      // todo only count yes's
      let updatedCountedVotes = context.CountedVotes.Add(voteResponse.NodeId, voteResponse.VoteGranted)  
      
      if updatedCountedVotes.Count >= majority then
        becomeLeader ()
      else
        let updatedContext = 
          {context with         
            CountedVotes = updatedCountedVotes}
        updatedContext
      
    let rec handle (context:Context) = async {      
      let! msg = inbox.Receive()                  
      log.Information("received message:{msg} current state: {state}", getMessageName(msg), getStateName(context.State))
      match msg, context.State with
      | ElectionTimeout, Follower 
      | ElectionTimeout, Candidate -> return! handle(startElection context)          
      | RpcCall (RequestVote voteRequest), Follower
      | RpcCall (RequestVote voteRequest), Candidate -> replyToVoteRequest voteRequest; return! handle(context)
      | RpcCall (RequestVoteResponse voteResponse), Candidate -> return! handle(receiveVoteResponse context voteResponse)
      | ElectionTimeout, Leader   
      | RpcCall _, _ -> return! handle(context)
    }

    handle(initialContext)
   
  member x.Start (cancellationToken : CancellationToken) = 
    let mailbox = new MailboxProcessor<RaftNotification>(notificationHandler)
    setupRemoteSubscriptions(mailbox)
    mailbox.Start()        
    cancellationToken.WaitHandle.WaitOne() |> ignore 