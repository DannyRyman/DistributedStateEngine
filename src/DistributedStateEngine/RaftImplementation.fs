module internal RaftImplementation
    open System.Threading
    open CommunicationTypes
    open TimerLibrary
    open Logging

    type private RaftNotification =
        | ElectionTimeout 
        | RpcIn of RpcIn    

    let private mailbox = new MailboxProcessor<RaftNotification>(fun inbox ->
        let receiveOrTimeout () =
            async {
                let! result = inbox.Receive(100) |> Async.Catch
                return
                  match result with
                  | Choice1Of2 r -> r
                  | Choice2Of2 e -> ElectionTimeout                
            }            

        let rec follower () =
            async {                 
                let! notification = receiveOrTimeout ()
                match notification with
                | ElectionTimeout -> 
                    log.Information "Election timeout received. Transitioning from follower -> candidate"
                    return! candidate()
                | RpcIn rpcIn -> log.Information "todo - implement rpc in (follower)"
                return! follower ()
            }

        and candidate () =
            async {                 
                let! notification = receiveOrTimeout ()
                match notification with
                | ElectionTimeout -> 
                    log.Information "todo - implement election timeout (candidate)"
                    return! candidate()
                | RpcIn rpcIn -> log.Information "todo - implement rpc in (candidate)"
                return! candidate ()
            }

        follower ())

    let init = 
        async {
            mailbox.Start ()            
        }