namespace StockCheck.Model.Test

open NUnit.Framework
open FsUnit
open StockCheck.ModelFs
open Ploeh.AutoFixture

type PeriodItemSetup () =
    member x.Fixture = new Fixture();
    member x.SalesItem = x.Fixture.Create<SalesItem>()
    member x.PeriodItem = new PeriodItem(x.SalesItem)

[<TestFixture>]
type ``Given that a PeriodItem has been constructed`` () =
    inherit PeriodItemSetup ()

    [<Test>] member x.
        ``The items received collection is initialised`` () =
        x.PeriodItem.ItemsReceived |> should be instanceOfType<System.Collections.Generic.List<ItemReceived>>

    [<Test>] member x.
        ``The items received collection is empty`` () =
        x.PeriodItem.ItemsReceived |> should be Empty

//[<TestFixture>]
//type ``Given that goods have been received`` () =
//    inherit PeriodItemSetup ()
//    let itemReceived = x.Fixture.Create<ItemReceived>()
//
//    [<Test>] member x.
//        ``The goods are added to the items received collection`` () =
//        true
