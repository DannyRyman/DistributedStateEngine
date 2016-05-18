
module internal RaftTypes
(*
[< CustomEquality >]
[< NoComparisonAttribute>]
type AppendEntryIndexes = {
  Index : uint64
  Term : uint64
}
with
  override this.Equals(other) = 
    match other with
    | null -> nullArg "other"
    | :? AppendEntryIndexes as i -> i.GetHashCode() = this.GetHashCode()
    | _ -> invalidArg "other" "Must be an instance of AppendEntryIndexes"    
  override this.GetHashCode() = (this.Index, this.Term).GetHashCode()

type LogEntry = {
  Indexes : AppendEntryIndexes
  Command : byte[]
}

type AppendEntries = 
  { Term : uint64
    LeaderId : string
    PreviousLogIndexes: Option<AppendEntryIndexes>
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

let getMessageName (message:RaftNotification) =
  match message with
  | ElectionTimeout -> "ElectionTimeout"
  | RpcCall rpc ->
    match rpc with 
    | RpcRequest (RequestVote _) -> "RequestVote"
    | RpcResponse (RequestVoteResponse _) -> "RequestVoteResponse"     
    | RpcRequest (AppendEntries _) -> "AppendEntries"
    | RpcResponse (AppendEntriesResponse _) -> "AppendEntriesResponse"

*)