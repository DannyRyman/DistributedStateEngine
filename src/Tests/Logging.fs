namespace Tests

module Logging =         
  open Serilog.Formatting.Display
  open Xunit.Abstractions
  open Serilog.Formatting
  open Serilog.Core
  open System.IO
  open Serilog
  open Serilog.Events

  type XUnitTestOutputSink(testOutputHelper:ITestOutputHelper,textFormatter:ITextFormatter) =
    interface ILogEventSink with
      member x.Emit logEvent =
        let renderSpace = new StringWriter()
        textFormatter.Format(logEvent, renderSpace)
        testOutputHelper.WriteLine(renderSpace.ToString())

  let createTestLoggerConfig testOutputHelper =
    let outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}"
    let formatter = new MessageTemplateTextFormatter(outputTemplate, null)
    let testOutputSink = new XUnitTestOutputSink(testOutputHelper, formatter) :> ILogEventSink
    let loggerConfig = new LoggerConfiguration()
    loggerConfig.WriteTo.Sink(testOutputSink, LevelAlias.Minimum) |> ignore
    loggerConfig