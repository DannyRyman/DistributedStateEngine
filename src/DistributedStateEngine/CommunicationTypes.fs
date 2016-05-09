module internal CommunicationTypes

open System

type AppendEntriesOut = 
  { Term : uint64
    LeaderId : string
    PrevLogIndex : uint64
    PrevLogTerm : uint64
    Entries : string []
    LeaderCommit : uint64 }

type AppendEntriesIn = 
  { Term : uint64
    Success : bool }

type RequestVoteOut = 
  { Term : uint64
    CandidateId : string
    LastLogIndex : uint64
    LastLogTerm : uint64 }

type RequestVoteIn = 
  { Term : uint64
    VoteGranted : bool }

type RpcIn = 
  | AppendEntries of AppendEntriesIn
  | RequestVote of RequestVoteIn