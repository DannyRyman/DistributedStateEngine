open System
open System.Text
open fszmq
open fszmq.Context
open fszmq.Socket
open System.Threading
open Communication
open Configuration

[<EntryPoint>]
let main argv =        
    

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

    let getDestinationNode () = 
        printfn "Enter the destination node:"
        Console.ReadLine()

    let rec loop() =        
        printfn "(u)nicast or (b)roadcast"
        let unicastOrBroadcast = Console.ReadLine()
        printfn "Enter the message:"
        let msg = Console.ReadLine()
        match unicastOrBroadcast with 
            | "u" -> unicast (getDestinationNode()) msg
            | "b" -> broadcast msg
            | _ -> printfn "invalid value, try again"
        loop()
    loop()

    0 // return an integer exit code