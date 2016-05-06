open System
open System.Text
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
        
        Socket.subscribe subscriber [| "ALL"B |]
        Socket.subscribe subscriber [| Encoding.ASCII.GetBytes(config.ThisNode.Port.ToString()) |]

        printfn "listening for broadcasts..."

        let rec loop() =
            // ignore the topic header
            Socket.recv subscriber |> ignore 

            let msg = Socket.recv subscriber |> Encoding.ASCII.GetString
            printfn "received: %s" msg
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
    
    let unicastMessage () =
        printfn "Enter the destination node name"
        let destinationNodeName = Console.ReadLine()
        publisher <~| Encoding.ASCII.GetBytes(destinationNodeName)
                  <<| "Foo"B

    let broadcastMessage () =
        printfn "attempting broadcast"
        publisher <~| "ALL"B
                  <<| "Bar"B

    let rec loop() =        
        printfn "(u)nicast or (b)roadcast"
        let unicastOrBroadcast = Console.ReadLine()
        match unicastOrBroadcast with 
            | "u" -> unicastMessage()
            | "b" -> broadcastMessage()
            | _ -> printfn "invalid value, try again"
        loop()
    loop()    
    
    0 // return an integer exit code