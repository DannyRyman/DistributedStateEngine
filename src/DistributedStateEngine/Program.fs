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
    
    let mailbox = new MailboxProcessor<string>(fun inbox ->
        let rec loop count =
            async { printfn "Message count = %d. Waiting for next message." count
                    let! msg = inbox.Receive()
                    printfn "Message received. %s" msg
                    return! loop( count + 1) }
        loop 0)

    mailbox.Start()

    Communication.setupRemoteSubscriptions mailbox

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