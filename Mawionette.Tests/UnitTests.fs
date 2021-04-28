namespace Mawionette.Tests
module UnitTests =
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


