namespace Mawionette.Tests

module IntegrationTests =

    open Xunit
    open Mawionette
    open Mawionette.RPA

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