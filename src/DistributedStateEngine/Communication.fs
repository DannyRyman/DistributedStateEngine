// Note that the first messages will potentially be lost due to the "slow joiner" sympton
// http://zguide.zeromq.org/page:all
module internal Communication

(*


open Configuration
open System.Text
open SerializationLibrary
open fszmq
open fszmq.Socket
open Microsoft.FSharp.Control
open Logging
open RaftTypes

let publisherContext = new Context()

let createPublisher() =     
  let publisher = Context.pub publisherContext
  printfn "creating publisher for port number: %i" config.ThisNode.Port
  Socket.bind publisher ("tcp://*:" + config.ThisNode.Port.ToString())  
  publisher

// Create the publisher
let private publisher = createPublisher()

let private publishMessage (destinationNode : string) (msg : RpcCall) =
  try
    log.Information ("Sending message {message} to {destination}", getMessageName(RpcCall msg), destinationNode)
    publisher <~| Encoding.ASCII.GetBytes(destinationNode) <<| (serializeToByteArray msg)
  with
    | :? ZMQError as ex -> log.Error("Zeromq Error {zmqErrorNumber} {exception}", ex.ErrorNumber, ex.ToString())
    | _ -> reraise()

// Public outbound communication functions
let unicast (destinationNode : string) = publishMessage destinationNode
let broadcast = publishMessage "ALL"

// Subscriber binding
let private createSubscription (mailboxProcessor : MailboxProcessor<RaftNotification>) = 
  async { 
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
      let msg = Socket.recv subscriber //|> Encoding.ASCII.GetString           
      let rpcCall = deserializeFromByteArray<RpcCall>(msg)
      mailboxProcessor.Post (RpcCall rpcCall)
      loop()
    loop()
  }

let setupRemoteSubscriptions (mailboxProcessor : MailboxProcessor<RaftNotification>) = 
  Async.Start(createSubscription mailboxProcessor)

*)