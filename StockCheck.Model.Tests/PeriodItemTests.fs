namespace StockCheck.Model.Test

open System
open NUnit.Framework
open FsUnit
open StockCheck.Model
open Ploeh.AutoFixture

module piSetup =
    let NewFixture () =
        new Fixture()

    let InitialiseSalesItem tax ctrSize saleUnit salePrice =
        let salesItem = (NewFixture ()).Create<SalesItem>()
        salesItem.TaxRate <- tax;
        salesItem.ContainerSize <- ctrSize;
        salesItem.SalesUnitsPerContainerUnit <- saleUnit;
        salesItem.SalesPrice <- salePrice;
        salesItem

    let initialisePeriodItem salesItem =
        let periodItem = new PeriodItem(salesItem)
        periodItem.OpeningStock <- 23.
        periodItem.ClosingStock <- 25.
        periodItem.ItemsReceived.Add(new ItemReceived(Quantity = 2., InvoicedAmountEx = 212.34M));
        periodItem.ItemsReceived.Add(new ItemReceived(Quantity = 1., InvoicedAmountEx = 109.3M));
        periodItem.ItemsReceived.Add(new ItemReceived(Quantity = 1., InvoicedAmountEx = 109.3M));
        periodItem

    let InitialiseDraughtSalesItem = InitialiseSalesItem 0.2 11. 8. 3.25M

    let InitialiseBottledSalesItem = InitialiseSalesItem 0.2 24. 1. 3.60M

    let InitialiseSnackSalesItem = InitialiseSalesItem 0. 48. 1. 0.60M

    let InitialiseSpiritSalesItem = InitialiseSalesItem 0.2 1. 20. 3.5M

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
    let salesItem = piSetup.InitialiseDraughtSalesItem
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
        ``The closing stock of the new item is zero`` () =
            periodItem2.ClosingStock |> should equal (0.)
            
    [<Test>] member x.
        ``The SalesItem of the new PeriodItem matches the source SalesItem`` () =
            periodItem2.SalesItem |> should be (sameAs periodItem1.SalesItem)

[<TestFixture>]
type ``Given that a PeriodItem has draught items received`` () =
    inherit PeriodItemSetup ()
    let periodItem = piSetup.InitialiseDraughtSalesItem |> piSetup.initialisePeriodItem
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

    [<Test>] member x.
        ``The SalesPerDay amount is correct`` () = 
            periodItem.SalesPerDay(new DateTime(2013, 04, 01), new DateTime(2013, 04, 03)) |> should equal (gallonsSold / 3.)

    [<Test>] member x.
        ``The DaysOnHand amount is correct`` () =
            periodItem.DaysOnHand(new DateTime(2013, 04, 01), new DateTime(2013, 04, 03)) |> should equal ((25. / (gallonsSold / 3.)) |> int)

    [<Test>] member x.
        ``The Profit is correct`` () =
            periodItem.Profit |> should equal (decimal pintsSold * periodItem.SalesItem.MarkUp)

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
    let salesItem = piSetup.InitialiseDraughtSalesItem
    let periodItem = new PeriodItem(salesItem)
    do
        periodItem.ItemsReceived.Add(new ItemReceived(Quantity = 2., InvoicedAmountEx = 212.34M));
        periodItem.ItemsReceived.Add(new ItemReceived(Quantity = 1., InvoicedAmountEx = 109.3M));
        periodItem.ItemsReceived.Add(new ItemReceived(Quantity = 1., InvoicedAmountInc = 120M));

    [<Test>] member x.
        ``The PurchasesTotal amount is correctly calculated`` () =
            periodItem.PurchasesTotal |> should equal (212.34M + 109.3M + (120M/((decimal)(1.0 + periodItem.SalesItem.TaxRate))))


[<TestFixture>]
type ``Given that a PeriodItem has bottles received`` () =
    inherit PeriodItemSetup ()
    let periodItem = piSetup.InitialiseBottledSalesItem |> piSetup.initialisePeriodItem
    let bottlesSold = 23. + (4. * 24.) - 25.

    [<Test>] member x.
        ``The ContainersReceived amount is correctly calculated`` () =
            periodItem.ContainersReceived |> should equal (2. + 1. + 1.)

    [<Test>] member x.
        ``The PurchasesEx amount is correctly calculated`` () =
            periodItem.PurchasesEx |> should equal (212.34M + 109.3M + 109.3M)

    [<Test>] member x.
        ``The Sales quantity is correct`` () =
            periodItem.Sales |> should equal bottlesSold

    [<Test>] member x.
        ``The SalesInc amount is correct`` () =
            periodItem.SalesInc |> should equal (decimal(bottlesSold) * 3.60M)

    [<Test>] member x.
        ``The SalesEx amount is correct`` () =
            periodItem.SalesEx |> should equal ((decimal(bottlesSold) * 3.60M) / decimal 1.2)

    [<Test>] member x.
        ``The CostOfSales amount is correct`` () =
            periodItem.CostOfSalesEx |> should equal (decimal periodItem.ContainersSold * periodItem.SalesItem.CostPerContainer)

    [<Test>] member x.
        ``The SalesPerDay amount is correct`` () = 
            periodItem.SalesPerDay(new DateTime(2013, 04, 01), new DateTime(2013, 04, 03)) |> should equal (bottlesSold / 3.)

    [<Test>] member x.
        ``The DaysOnHand amount is correct`` () =
            periodItem.DaysOnHand(new DateTime(2013, 04, 01), new DateTime(2013, 04, 03)) |> should equal ((25. / (bottlesSold / 3.)) |> int)

[<TestFixture>]
type ``Given that a PeriodItem has snacks received`` () =
    inherit PeriodItemSetup ()
    let periodItem = piSetup.InitialiseSnackSalesItem |> piSetup.initialisePeriodItem
    let packetsSold = 23. + (4. * 48.) - 25.

    [<Test>] member x.
        ``The ContainersReceived amount is correctly calculated`` () =
            periodItem.ContainersReceived |> should equal (2. + 1. + 1.)

    [<Test>] member x.
        ``The PurchasesEx amount is correctly calculated`` () =
            periodItem.PurchasesEx |> should equal (212.34M + 109.3M + 109.3M)

    [<Test>] member x.
        ``The Sales quantity is correct`` () =
            periodItem.Sales |> should equal packetsSold

    [<Test>] member x.
        ``The SalesInc amount is correct`` () =
            periodItem.SalesInc |> should equal (decimal(packetsSold) * 0.60M)

    [<Test>] member x.
        ``The SalesEx amount is correct`` () =
            periodItem.SalesEx |> should equal ((decimal(packetsSold) * 0.60M) / decimal 1.0)

    [<Test>] member x.
        ``The CostOfSales amount is correct`` () =
            periodItem.CostOfSalesEx |> should equal (decimal periodItem.ContainersSold * periodItem.SalesItem.CostPerContainer)

    [<Test>] member x.
        ``The SalesPerDay amount is correct`` () = 
            periodItem.SalesPerDay(new DateTime(2013, 04, 01), new DateTime(2013, 04, 03)) |> should equal (packetsSold / 3.)

    [<Test>] member x.
        ``The DaysOnHand amount is correct`` () =
            periodItem.DaysOnHand(new DateTime(2013, 04, 01), new DateTime(2013, 04, 03)) |> should equal ((25. / (packetsSold / 3.)) |> int)

[<TestFixture>]
type ``Given that a PeriodItem has spirits received`` () =
    inherit PeriodItemSetup ()
    let periodItem = piSetup.InitialiseSpiritSalesItem |> piSetup.initialisePeriodItem
    let bottlesSold = 23. + (4. * 1.) - 25.
    let measuresSold = bottlesSold * (0.7 /0.035)

    [<Test>] member x.
        ``The ContainersReceived amount is correctly calculated`` () =
            periodItem.ContainersReceived |> should equal (2. + 1. + 1.)

    [<Test>] member x.
        ``The PurchasesEx amount is correctly calculated`` () =
            periodItem.PurchasesEx |> should equal (212.34M + 109.3M + 109.3M)

    [<Test>] member x.
        ``The Sales quantity is correct`` () =
            periodItem.Sales |> should equal bottlesSold

    [<Test>] member x.
        ``The SalesInc amount is correct`` () =
            periodItem.SalesInc |> should equal (decimal(measuresSold) * 3.5M)

    [<Test>] member x.
        ``The SalesEx amount is correct`` () =
            periodItem.SalesEx |> should equal ((decimal(measuresSold) * 3.5M) / decimal 1.2)

    [<Test>] member x.
        ``The CostOfSales amount is correct`` () =
            periodItem.CostOfSalesEx |> should equal (decimal (2) * periodItem.SalesItem.CostPerContainer)

    [<Test>] member x.
        ``The SalesPerDay amount is correct`` () = 
            periodItem.SalesPerDay(new DateTime(2013, 04, 01), new DateTime(2013, 04, 03)) |> should equal (bottlesSold / 3.)

    [<Test>] member x.
        ``The DaysOnHand amount is correct`` () =
            periodItem.DaysOnHand(new DateTime(2013, 04, 01), new DateTime(2013, 04, 03)) |> should equal ((25. / (bottlesSold / 3.)) |> int)
