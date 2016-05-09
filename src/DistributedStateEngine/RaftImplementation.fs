module internal RaftImplementation

open System.Threading
open CommunicationTypes
open TimerLibrary
open Logging

type private RaftNotification = 
  | ElectionTimeout
  | RpcIn of RpcIn

let private raftState (inbox:MailboxProcessor<RaftNotification>) =
  let receiveOrTimeout() = 
    async { 
      // todo randomise the timeout
      let! result = inbox.Receive(100) |> Async.Catch
      return match result with
             | Choice1Of2 r -> r
             | Choice2Of2 e -> ElectionTimeout
    }
  
  let rec follower() = 
    async { 
      let! notification = receiveOrTimeout()
      match notification with
      | ElectionTimeout -> 
        log.Information "Election timeout received. Transitioning from follower -> candidate"
        return! candidate()
      | RpcIn _ -> log.Information "todo - implement rpc in (follower)"
      return! follower()
    }
  
  and candidate() = 
    async { 
      let! notification = receiveOrTimeout()
      match notification with
      | ElectionTimeout -> 
        log.Information "todo - implement election timeout (candidate)"
        return! candidate()
      | RpcIn _ -> log.Information "todo - implement rpc in (candidate)"
      return! candidate()
    }
 
  follower()

let private mailbox = 
  new MailboxProcessor<RaftNotification>(raftState)

let init = async { mailbox.Start() }