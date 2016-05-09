module internal CommunicationTypes
    open System

    type AppendEntriesOut = { term:uint64; leaderId:string; prevLogIndex:uint64; prevLogTerm:uint64; entries:string[]; leaderCommit:uint64 }

    type AppendEntriesIn = { term:uint64; success:bool }

    type RequestVoteOut = { term:uint64; candidateId:string; lastLogIndex:uint64; lastLogTerm:uint64; }

    type RequestVoteIn = { term:uint64; voteGranted:bool }

    type RpcResponse =
        | AppendEntries of AppendEntriesIn
        | RequestVote of RequestVoteIn