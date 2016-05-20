namespace Tests

open Raft

module Fakes =
  open TimerLibrary

  type FakeTimeoutService() = 
    let evt = Event<_>()

    let recordedCommands = new ResizeArray<TimeoutServiceOperations>() 

    member x.TriggerTimeout() = evt.Trigger()
    member x.RecordedValues = recordedCommands

    interface ITimeoutService with
      member x.Start() = recordedCommands.Add(Start)
      member x.Stop() = recordedCommands.Add(Stop)
      member x.Reset() = recordedCommands.Add(Reset)
      member x.TimedOut = evt.Publish :> _

type NullWorkflow() =
  interface IWorkflow with 
    member this.ProcessRaftEvent x = 
      let initialContext, raftEvent = x
      initialContext
