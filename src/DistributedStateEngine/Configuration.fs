module internal Configuration
    open FSharp.Configuration

    type Config = YamlConfig<"config.yaml">

    let config = new Config()
    config.Load("config.yaml")

