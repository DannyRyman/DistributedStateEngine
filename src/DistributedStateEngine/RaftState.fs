module internal RaftState

open FSharp.Data.Sql
open RaftTypes
open System

let [<Literal>] ResolutionPath = __SOURCE_DIRECTORY__ + @"\bin\debug" 
let [<Literal>] ConnectionString = "Data Source=" + __SOURCE_DIRECTORY__ + @"\log.db;Version=3"

type Sql = SqlDataProvider< 
              ConnectionString = ConnectionString,
              DatabaseVendor = Common.DatabaseProviderTypes.SQLITE,
              ResolutionPath = ResolutionPath,
              IndividualsAmount = 1000,
              UseOptionTypes = true >

type UpdateType = 
| IncrementTerm 
| ResetTermTo of int64
| AppendLogEntries of LogEntry[]  

let saveLogEntries (entries:LogEntry[]) =   
  async {
    let ctx = Sql.GetDataContext ()  
    
    for entry in Array.rev entries do
      let newLog = ctx.Main.Log.Create()
      newLog.Id <- entry.Indexes.Index |> Convert.ToInt64
      newLog.Term <- entry.Indexes.Term |> Convert.ToInt64
      newLog.Command <- entry.Command

    do! ctx.SubmitUpdatesAsync ()
  }

let getLastLogEntry () = 
  let ctx = Sql.GetDataContext ()  
  query { for l in ctx.Main.Log do select {Indexes = {Index = l.Id |> Convert.ToUInt64; Term = l.Term |> Convert.ToUInt64}; Command = l.Command}}
  |> Seq.tryLast   
  
(*
let updater =
  MailboxProcessor.Start(fun inbox -> 
    let rec loop() =
      async {
        let! msg = inbox.Receive()
        match msg with 
        | AppendLogEntries entries -> do! saveLogEntriesLocal entries          
        | IncrementTerm | ResetTermTo _ -> failwith "todo - implement"
        return! loop()
      }
    loop()
  )
  *)