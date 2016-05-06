open System
open fszmq
open fszmq.Context
open fszmq.Socket
open System.Threading
open FSharp.Configuration

type Config = YamlConfig<"config.yaml">

[<EntryPoint>]
let main argv =        
    let config = new Config()
    config.Load("config.yaml")

    printfn "libzmq version: %A" ZMQ.version    

    let broadcastSubscriber context = async {
        use subscriber = Context.sub context

        for otherNode in config.OtherNodes do     
            printfn "subscribing to %s" otherNode   
            Socket.connect subscriber ("tcp://" + otherNode)

        Socket.subscribe subscriber [| ""B |]

        printfn "listening for broadcasts..."

        let rec loop() =
            let msg = Socket.recv subscriber
            printfn "message received"
            loop()
        loop()
    }
    
    use subscriberContext = new Context()
    Async.Start (broadcastSubscriber subscriberContext)


    // publisher
    use publisherContext = new Context()
    use publisher = Context.pub publisherContext
    Socket.setOption publisher (ZMQ.SNDHWM,1100000)
    printfn "creating publisher for port number: %i" config.ThisNode.Port
    Socket.bind publisher ("tcp://*:" + config.ThisNode.Port.ToString())
    
    let rec loop() =        
        printfn "Press a key to send a message"
        Console.ReadLine() |> ignore
        "Rhubarb"B |> Socket.send publisher
        loop()
    loop()    
    
    0 // return an integer exit code