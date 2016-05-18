﻿namespace Tests


module RaftFakes =
  open TimerLibrary

  type RecordedTimeoutServiceCommand =
    | Start
    | Stop
    | Reset

  type FakeTimeoutService() = 
    let evt = Event<_>()

    let recordedCommands = new ResizeArray<RecordedTimeoutServiceCommand>() 

    member x.TriggerTimeout() = evt.Trigger()
    member x.RecordedValues = recordedCommands

    interface ITimeoutService with
      member x.Start() = recordedCommands.Add(Start)
      member x.Stop() = recordedCommands.Add(Stop)
      member x.Reset() = recordedCommands.Add(Reset)
      member x.TimedOut = evt.Publish :> _

