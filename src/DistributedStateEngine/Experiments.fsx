let mutable fn = fun _ -> printfn "test"
fn <- fun _ -> printfn "test2"
fn()
