// Note that the first messages will potentially be lost due to the "slow joiner" sympton
// http://zguide.zeromq.org/page:all

module internal Communication
open System
open Configuration
open FSharp.Configuration
open System.Text
open System.Threading
open fszmq
open fszmq.Socket
open Microsoft.FSharp.Control

let private createPublisher () =
    let publisherContext = new Context()
    let publisher = Context.pub publisherContext
    Socket.setOption publisher (ZMQ.SNDHWM,1100000)
    printfn "creating publisher for port number: %i" config.ThisNode.Port
    Socket.bind publisher ("tcp://*:" + config.ThisNode.Port.ToString())
    publisher

// Create the publisher
let private publisher = createPublisher()
    
// Private communication functions
let private unicastMessage (publisher:Socket) (destinationNode:string) (msg:string)  =
    publisher <~| Encoding.ASCII.GetBytes(destinationNode)
              <<| Encoding.ASCII.GetBytes(msg)
    
let private broadcastMessage (publisher:Socket) (msg:string) =
    publisher <~| "ALL"B
              <<| Encoding.ASCII.GetBytes(msg)

// Public outbound communication functions
let unicast destinationNode msg =
    unicastMessage publisher destinationNode msg

let broadcast msg =
    broadcastMessage publisher msg

// Subscriber binding
let private createSubscription (mailboxProcessor:MailboxProcessor<string>) = async {
    use subscriberContext = new Context()
    use subscriber = Context.sub subscriberContext

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
        mailboxProcessor.Post msg
        loop()
    loop()
}

let setupRemoteSubscriptions (mailboxProcessor:MailboxProcessor<string>) =
    Async.Start (createSubscription mailboxProcessor)