#r @"C:\Users\danielr\Source\Repos\DistributedStateEngine\src\DistributedStateEngine\bin\Debug\FSharp.Data.SqlProvider.dll"
open FSharp.Data.Sql

let [<Literal>] ResolutionPath = __SOURCE_DIRECTORY__ + @"\bin\debug" 
let [<Literal>] ConnectionString = "Data Source=" + __SOURCE_DIRECTORY__ + @"\log.db;Version=3"

type Sql = SqlDataProvider< 
              ConnectionString = ConnectionString,
              DatabaseVendor = Common.DatabaseProviderTypes.SQLITE,
              ResolutionPath = ResolutionPath,
              IndividualsAmount = 1000,
              UseOptionTypes = true >
  
let ctx = Sql.GetDataContext ()

type LogEntry = {
  Id : int64
  Term: int64
  Command: byte[]
}

let saveLog (entry:LogEntry) = 
  let newLog = ctx.Main.Log.Create()
  newLog.Id <- entry.Id
  newLog.Term <- entry.Term
  newLog.Command <- entry.Command
  ctx.SubmitUpdates ()
    
let newLog = {Id = 1L; Term = 1L; Command = Array.empty}

saveLog (newLog)


 //ctx.SubmitUpdates
 