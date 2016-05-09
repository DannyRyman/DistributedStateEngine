module internal RaftImplementation
    open System.Threading
    open CommunicationTypes
    open TimerLibrary

    type RaftNotification =
        | ElectionTimeout 
        | RpcIn of RpcIn

    let mailbox = new MailboxProcessor<RaftNotification>(fun inbox ->
        let rec follower () =
            async { 
                let! notification = inbox.Receive()
                match notification with
                | ElectionTimeout -> printfn "todo - implement election timeout"
                | RpcIn rpcIn -> printfn "todo - implement rpc in"
                return! follower ()
            }
        follower ())    

    let electionTimeout () =         
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