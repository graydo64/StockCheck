namespace StockCheck.Model.Test

open NUnit.Framework
open FsUnit
open StockCheck.Model
open Ploeh.AutoFixture
open System
open StockCheck.Model.Factory

module TestUtils =
    let Zeroise (period : Period) =
        let head = period.Items |> Seq.head
        let tail = period.Items |> Seq.skip 1
        let newHead = { head with OpeningStock = 0.; ClosingStock = 0.; ItemsReceived = [] }
        let items = Seq.append [newHead] tail
        { period with Items = items }

[<TestFixture>]
type ``Given that a Period has been constructed`` () = 
    let period = Factory.getPeriod (Guid.NewGuid().ToString()) "..." (DateTime.Now) (DateTime.Now.AddDays(30.)) //new Period()
    let fixture = new Fixture()

    [<Test>] member x.
        ``The Items collection should be empty`` () =
            Seq.length (period.Items) |> should equal (0)

    [<Test>] member x.
        ``The period start date should start at midnight`` () =
            period.StartOfPeriod.TimeOfDay |> should equal (new TimeSpan(0, 0, 0))

    [<Test>] member x.
        ``The period end date should end at a second to midnight`` () =
            period.EndOfPeriod.TimeOfDay |> should equal (new TimeSpan(0, 0, 0))

[<TestFixture>]
type ``Given that a Period has been initialised from an existing Period`` () =
    let fixture = new Fixture()
    let source = fixture.Create<Period>()
    let target = initialisePeriodFromClone source

    [<Test>] member x.
        ``The items collection contains the same number of items as the source`` () =
            Seq.length target.Items |> should equal (Seq.length source.Items)

    [<Test>] member x.
        ``The items have the same salesItem as the source items`` () =
            Seq.exists2 (fun (a : PeriodItem) (b : PeriodItem) -> a.SalesItem = b.SalesItem) source.Items target.Items |> should be True

    [<Test>] member x.
        ``The period start date should be one day after the source period end date`` () =
            target.StartOfPeriod |> should equal (source.EndOfPeriod.Date.AddDays(1.))

    [<Test>] member x.
        ``The period start date should start at midnight`` () =
            target.StartOfPeriod.TimeOfDay |> should equal (new TimeSpan(0, 0, 0))

    [<Test>] member x.
        ``The period end date should be midnight of the last day of the period`` () =
            target.EndOfPeriod.TimeOfDay |> should equal (new TimeSpan(0, 0, 0))

[<TestFixture>]
type ``Given that a period has been initialised without zero item carried stock`` () =
    let fixture = new Fixture()
    let source = fixture.Create<Period>()
    let target = initialiseWithoutZeroCarriedItems (TestUtils.Zeroise source)

    [<Test>] member x.
        ``The period's item collection should exclude items with zero opening and closing stocks`` () =
            
            Seq.length target.Items |> should equal (Seq.length source.Items - 1)

    [<Test>] member x.
        ``The period start date should be one day after the source period end date`` () =
            target.StartOfPeriod |> should equal (source.EndOfPeriod.Date.AddDays(1.))
        