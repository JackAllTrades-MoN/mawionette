module Mawionette.Tests

open Xunit
open Mawionette
open Mawionette.RPA

[<Fact>]
let ``Key up and down`` () =
    rpa {
        do! typeKey KeyKind.ARROW_UP
        do! typeKey KeyKind.ARROW_DOWN
    } |> exec
    Assert.True true

[<Fact>]
let ``run quietly`` () =
    let waitOffset, maxScale = 5000, 3
    let mainLoop = rpa {
        do! typeKey KeyKind.ARROW_DOWN
        do! typeKey KeyKind.ARROW_UP
        let! cnt = loopCount
        if cnt > 5 then do! stopLoop
    }
    rpa {
        do! startLoggingToStdout
        do! info "Quiet Mode"
        do! waitAndContinue waitOffset maxScale mainLoop
    } |> exec
    Assert.True true
