#r @"C:\Users\danielr\Source\Repos\DistributedStateEngine\DistributedStateEngine\bin\Debug\fszmq.dll"
open fszmq
open System.Threading

let configureNode allNodes currentNode =
    let otherNodes = Array.except[|currentNode|] allNodes
    printfn "current: %i other:%A" currentNode otherNodes

let quorumSize = 3
let quorumPorts = [|5561..5561+quorumSize-1|]
quorumPorts |> Array.iter(configureNode (quorumPorts))