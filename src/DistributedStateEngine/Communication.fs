// Note that the first messages will potentially be lost due to the "slow joiner" sympton
// http://zguide.zeromq.org/page:all

module internal Communication
open FSharp.Configuration
open System.Text
open System.Threading
open fszmq
open fszmq.Socket

type Config = YamlConfig<"config.yaml">

let config = new Config()
config.Load("config.yaml")

let publisherContext = new Context()
let publisher = Context.pub publisherContext
Socket.setOption publisher (ZMQ.SNDHWM,1100000)
printfn "creating publisher for port number: %i" config.ThisNode.Port
Socket.bind publisher ("tcp://*:" + config.ThisNode.Port.ToString())
    
// Private communication functions
let private unicastMessage (publisher:Socket) (destinationNode:string) (msg:string)  =
    publisher <~| Encoding.ASCII.GetBytes(destinationNode)
              <<| Encoding.ASCII.GetBytes(msg)
    
let private broadcastMessage (publisher:Socket) (msg:string) =
    publisher <~| "ALL"B
              <<| Encoding.ASCII.GetBytes(msg)

// Public communication functions
let unicast destinationNode msg =
    unicastMessage publisher destinationNode msg

let broadcast msg =
    broadcastMessage publisher msg