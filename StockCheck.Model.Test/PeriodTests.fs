namespace StockCheck.Model.Test

open NUnit.Framework
open FsUnit
open StockCheck.ModelFs
open Ploeh.AutoFixture

[<TestFixture>]
type ``Given that a Period has been constructed`` () = 
    let period = new Period()
    let fixture = new Fixture()

    [<Test>] member x.
        ``The Items collection should be a list of PeriodItems`` () =
            period.Items |> should be instanceOfType<System.Collections.Generic.List<PeriodItem>>

[<TestFixture>]
type ``Given that a Period has been initialised from an existing Period`` () =
    let fixture = new Fixture()
    let source = fixture.Create<Period>()
    let target = Period.InitialiseFromClone source

    [<Test>] member x.
        ``The items collection contains the same number of items as the source`` () =
            target.Items |> should haveCount source.Items.Count

    [<Test>] member x.
        ``The items have the same salesItem as the source items`` () =
            Seq.exists2 (fun (a : PeriodItem) (b : PeriodItem) -> a.SalesItem = b.SalesItem) source.Items target.Items |> should be True

    [<Test>] member x.
        ``The period start date should be one day after the source period end date`` () =
            target.StartOfPeriod |> should equal (source.EndOfPeriod.AddDays(1.))

[<TestFixture>]
type ``Given that a period has been initialised without zero item carried stock`` () =
    let fixture = new Fixture()
    let source = fixture.Create<Period>()
    let target = Period.InitialiseWithoutZeroCarriedItems source
    do
        (source.Items |> Seq.head).OpeningStock <- 0.
        