module internal RaftImplementation
    open System.Threading
    open CommunicationTypes
    open TimerLibrary
    open Logging

    type private RaftNotification =
        | ElectionTimeout 
        | RpcIn of RpcIn

    let private mailbox = new MailboxProcessor<RaftNotification>(fun inbox ->
        let rec follower () =
            async { 
                let! notification = inbox.Receive()
                match notification with
                | ElectionTimeout -> log.Information "todo - implement election timeout (follower)"
                | RpcIn rpcIn -> log.Information "todo - implement rpc in"
                return! follower ()
            }
        follower ())    

    let private electionTimeout () =         
        async {
            mailbox.Post ElectionTimeout
        }

    let init = 
        async {
            mailbox.Start ()

            let source = new CancellationTokenSource();
            let cancellationToken = source.Token;
            // todo: get election timeout from configuration + add random element
            do! DoPeriodicWork (electionTimeout) 100 cancellationToken           
        }