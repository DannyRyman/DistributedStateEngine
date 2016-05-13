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

type RpcRequest =   
  | RequestVote of RequestVote  
  | AppendEntries of AppendEntries  

type RpcResponse =
  | RequestVoteResponse of RequestVoteResponse  
  | AppendEntriesResponse of AppendEntriesResponse

type RpcCall =
  | RpcRequest of RpcRequest
  | RpcResponse of RpcResponse 

type RaftNotification = 
  | ElectionTimeout
  | RpcCall of RpcCall