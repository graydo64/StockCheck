namespace StockCheck.Model.Test

open NUnit.Framework
open FsUnit
open StockCheck.ModelFs
open Ploeh.AutoFixture

module piSetup =
    let NewFixture () =
        new Fixture()

    let InitialiseSalesItem () =
        let salesItem = (NewFixture ()).Create<SalesItem>()
        salesItem.TaxRate <- 0.2;
        salesItem.ContainerSize <- 11.;
        salesItem.UnitOfSale <- 1. / 8.;
        salesItem.SalesPrice <- 3.25M;
        salesItem

    let initialisePeriodItem salesItem =
        let periodItem = new PeriodItem(salesItem)
        periodItem.OpeningStock <- 23.
        periodItem.ClosingStock <- 25.
        periodItem.ItemsReceived.Add(new ItemReceived(Quantity = 2., InvoicedAmountEx = 212.34M));
        periodItem.ItemsReceived.Add(new ItemReceived(Quantity = 1., InvoicedAmountEx = 109.3M));
        periodItem.ItemsReceived.Add(new ItemReceived(Quantity = 1., InvoicedAmountEx = 109.3M));
        periodItem


type PeriodItemSetup () =
    member x.Fixture = piSetup.NewFixture ()
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

[<TestFixture>]
type ``Given that goods have been received`` () as this =
    inherit PeriodItemSetup ()
    let itemReceived = this.Fixture.Create<ItemReceived>()
    let salesItem = piSetup.InitialiseSalesItem()
    let periodItem = new PeriodItem(salesItem)
    do
        periodItem.ReceiveItems itemReceived.ReceivedDate itemReceived.Quantity itemReceived.InvoicedAmountEx itemReceived.InvoicedAmountInc

    [<Test>] member x.
        ``The goods are added to the items received collection`` () =
            periodItem.ItemsReceived |> should haveCount 1

[<TestFixture>]
type ``Given that a PeriodItem is being copied for the next period`` () as this =
    inherit PeriodItemSetup ()
    let periodItem1 = this.Fixture.Create<PeriodItem>()
    let periodItem2 = periodItem1.CopyForNextPeriod()

    [<Test>] member x.
        ``A new PeriodItem is created`` () =
            periodItem2 |> should not' (be sameAs periodItem1)

    [<Test>] member x.
        ``The opening stock of the new item matches the closing stock of the source item`` () =
            periodItem2.OpeningStock |> should equal periodItem1.ClosingStock
            
    [<Test>] member x.
        ``The SalesItem of the new PeriodItem matches the source SalesItem`` () =
            periodItem2.SalesItem |> should be (sameAs periodItem1.SalesItem)

[<TestFixture>]
type ``Given that a PeriodItem has draught items received`` () =
    inherit PeriodItemSetup ()
    let periodItem = piSetup.InitialiseSalesItem() |> piSetup.initialisePeriodItem
    let gallonsSold = 23. + (4. * 11.) - 25.
    let pintsSold = 8. * gallonsSold

    [<Test>] member x.
        ``The ContainersReceived amount is correctly calculated`` () =
            periodItem.ContainersReceived |> should equal (2. + 1. + 1.)

    [<Test>] member x.
        ``The PurchasesEx amount is correctly calculated`` () =
            periodItem.PurchasesEx |> should equal (212.34M + 109.3M + 109.3M)

    [<Test>] member x.
        ``The Sales quantity is correct`` () =
            periodItem.Sales |> should equal gallonsSold

    [<Test>] member x.
        ``The SalesInc amount is correct`` () =
            periodItem.SalesInc |> should equal (decimal(pintsSold) * 3.25M)

    [<Test>] member x.
        ``The SalesEx amount is correct`` () =
            periodItem.SalesEx |> should equal ((decimal(pintsSold) * 3.25M) / decimal 1.2)

    [<Test>] member x.
        ``The CostOfSales amount is correct`` () =
            periodItem.CostOfSalesEx |> should equal (decimal periodItem.ContainersSold * periodItem.SalesItem.CostPerContainer)

[<TestFixture>]
type ``Given that a PeriodItem has Tax inclusive draught items received`` () as this =
    inherit PeriodItemSetup ()
    let periodItem = this.Fixture.Create<SalesItem> () |> piSetup.initialisePeriodItem
    do
        periodItem.ItemsReceived.Add(new ItemReceived(Quantity = 2., InvoicedAmountInc = 212.34M));
        periodItem.ItemsReceived.Add(new ItemReceived(Quantity = 1., InvoicedAmountInc = 109.3M));
        periodItem.ItemsReceived.Add(new ItemReceived(Quantity = 1., InvoicedAmountInc = 109.3M));

    [<Test>] member x.
        ``The PurchasesInc amount is correctly calculated`` () =
            periodItem.PurchasesInc |> should equal (212.34M + 109.3M + 109.3M)

[<TestFixture>]
type ``Given that a PeriodItem has mixed Tax Exclusive and inclusive draught items received`` () =
    inherit PeriodItemSetup ()
    let salesItem = piSetup.InitialiseSalesItem ()
    let periodItem = new PeriodItem(salesItem)
    do
        periodItem.ItemsReceived.Add(new ItemReceived(Quantity = 2., InvoicedAmountEx = 212.34M));
        periodItem.ItemsReceived.Add(new ItemReceived(Quantity = 1., InvoicedAmountEx = 109.3M));
        periodItem.ItemsReceived.Add(new ItemReceived(Quantity = 1., InvoicedAmountInc = 120M));

    [<Test>] member x.
        ``The PurchasesTotal amount is correctly calculated`` () =
            periodItem.PurchasesTotal |> should equal (212.34M + 109.3M + (120M/((decimal)(1.0 + periodItem.SalesItem.TaxRate))))