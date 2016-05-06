#r @"src/packages/FAKE/tools/FakeLib.dll"
#r @"src/packages/FSharp.Configuration.0.5.12/lib/net40/FSharp.Configuration.dll"

open Fake
open FSharp.Configuration

let buildDir = "./.build"
let multiNodeSetupRootDir = "./MultiNodeDevSetup/"

type Config = YamlConfig<"src\DistributedStateEngine\config.yaml">

let customiseNodeConfig otherNodes currentNode = 
    tracefn "updating configuration for node %i" currentNode
    let config = new Config()
    config.Load(buildDir + "/config.yaml")
    config.ThisNode.Port <- currentNode    
    config.OtherNodes <- otherNodes |> Array.map(fun a -> "localhost:" + a.ToString())
    config.Save()  
    tracefn "configuration updated to \n %s" (config.ToString()) 

let createNodeApplicationFolder node =    
    let nodeDestinationDirectory = multiNodeSetupRootDir + "node" + node.ToString()
    tracefn "Attempting to create a node application folder at %s" nodeDestinationDirectory
    XCopy buildDir nodeDestinationDirectory

let configureNode allNodes currentNode =
    let otherNodes = Array.except[|currentNode|] allNodes
    tracefn "configuring node %i" currentNode
    customiseNodeConfig otherNodes currentNode
    createNodeApplicationFolder currentNode

Target "Clean" (fun _ -> 
    CleanDir buildDir
)

Target "CleanPreviousMultiNodeDevSetup" (fun _ -> 
    CleanDir multiNodeSetupRootDir
)

Target "BuildApp" (fun _ -> 
    !! "src\DistributedStateEngine.sln"
        |> MSBuildRelease buildDir "Build"
        |> Log "AppBuild-Output: "
)

Target "Default" (fun _ ->
    trace "Hello world from FAKE"
)

Target "MultiNodeDevSetup" (fun _ -> 
    trace "Performing multi node developer setup"
    let quorumSize = 3
    let quorumPorts = [|5561..5561+quorumSize-1|]
    quorumPorts |> Array.iter(configureNode (quorumPorts))
)

"Clean"
    ==> "BuildApp"
    ==> "Default"

"Clean" 
    ==> "CleanPreviousMultiNodeDevSetup"
    ==> "BuildApp"
    ==> "MultiNodeDevSetup"

RunTargetOrDefault "Default"