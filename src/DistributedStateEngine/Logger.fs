module Logging
    open Serilog
        
    let private getLoggerConfiguration () =
        let config = new LoggerConfiguration()
        config.WriteTo.ColoredConsole() |> ignore
        config

    let log = (getLoggerConfiguration()).CreateLogger()
    
    