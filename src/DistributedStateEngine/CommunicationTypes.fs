module internal CommunicationTypes

open System

type AppendEntries = 
  { Term : uint64
    LeaderId : string
    PrevLogIndex : uint64
    PrevLogTerm : uint64
    Entries : string []
    LeaderCommit : uint64 }

type AppendEntriesResponse = 
  { Term : uint64
    Success : bool }

type RequestVote = 
  { Term : uint64
    CandidateId : string
    LastLogIndex : uint64
    LastLogTerm : uint64 }

type RequestVoteResponse = 
  { NodeId: string
    Term : uint64
    VoteGranted : bool }

type RpcCall =   
  | RequestVote of RequestVote
  | RequestVoteResponse of RequestVoteResponse  
  | AppendEntries of AppendEntries
  | AppendEntriesResponse of AppendEntriesResponse

type RaftNotification = 
  | ElectionTimeout
  | RpcCall of RpcCall