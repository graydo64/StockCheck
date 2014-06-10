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
        salesItem.SalesUnitType <- saleUnit;
        salesItem.SalesPrice <- salePrice;
        salesItem

    let initialisePeriodItem salesItem =
        let periodItem = new PeriodItem(salesItem)
        periodItem.OpeningStock <- 23.
        periodItem.ClosingStock <- 25.
        periodItem.ItemsReceived.Add(new ItemReceived(Quantity = 2., InvoicedAmountEx = decimal 2. * salesItem.CostPerContainer));
        periodItem.ItemsReceived.Add(new ItemReceived(Quantity = 1., InvoicedAmountEx = decimal 1. * salesItem.CostPerContainer));
        periodItem.ItemsReceived.Add(new ItemReceived(Quantity = 1., InvoicedAmountEx = decimal 1. * salesItem.CostPerContainer));
        periodItem

    let InitialiseDraughtSalesItem () = InitialiseSalesItem 0.2 11. Pint 3.25M

    let InitialiseBottledSalesItem () = InitialiseSalesItem 0.2 24. Unit 3.60M

    let InitialiseSnackSalesItem () = InitialiseSalesItem 0. 48. Unit 0.60M

    let InitialiseSpiritSalesItem () = InitialiseSalesItem 0.2 0.7 Spirit 3.5M

    let InitialiseWineSalesItem () = InitialiseSalesItem 0.2 0.75 Wine 3.45M

type PeriodItemSetup () =
    member x.Fixture = piSetup.NewFixture ()
    member x.SalesItem = x.Fixture.Create<SalesItem>()
    member x.PeriodItem = new PeriodItem(x.SalesItem)
    member x.d0 = new DateTime(14, 6, 4)
    member x.dx = 3.
    member x.d1 = x.d0.AddDays(x.dx - 1.)


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
    let salesItem = piSetup.InitialiseDraughtSalesItem ()
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
type ``Given that a PeriodItem has draught items received`` () as this =
    inherit PeriodItemSetup ()
    let periodItem = piSetup.InitialiseDraughtSalesItem () |> piSetup.initialisePeriodItem
    let gallonsOnHand = 25.
    let gallonsSold = 23. + (4. * 11.) - gallonsOnHand
    let pintsSold = 8. * gallonsSold

    [<Test>] member x.
        ``The ContainersReceived amount is correctly calculated`` () =
            periodItem.ContainersReceived |> should equal (2. + 1. + 1.)

    [<Test>] member x.
        ``The PurchasesEx amount is correctly calculated`` () =
            periodItem.PurchasesEx |> should equal (decimal 4. * periodItem.SalesItem.CostPerContainer)

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
            Utils.Round2M (periodItem.CostOfSalesEx) |> should equal (Utils.Round2M (decimal pintsSold * periodItem.SalesItem.CostPerUnitOfSale))

    [<Test>] member x.
        ``The SalesPerDay amount is correct`` () = 
            periodItem.SalesPerDay(this.d0, this.d1) |> should equal (gallonsSold / this.dx)

    [<Test>] member x.
        ``The DaysOnHand amount is correct`` () =
            periodItem.DaysOnHand(this.d0, this.d1) |> should equal ((25. / (gallonsSold / this.dx)) |> int)

    [<Test>] member x.
        ``The Profit is correct`` () =
            periodItem.Profit |> should equal (decimal pintsSold * periodItem.SalesItem.MarkUp)

    [<Test>] member x.
        ``The Closing Value at Sales Inc should be correct`` () =
            periodItem.ClosingValueSalesInc |> should equal (decimal (gallonsOnHand * 8.) * periodItem.SalesItem.SalesPrice)

    [<Test>] member x.
        ``The Closing Value at Sales Ex should be correct`` () =
            periodItem.ClosingValueSalesEx |> should equal ((decimal (gallonsOnHand * 8.) * periodItem.SalesItem.SalesPrice) / decimal 1.2)

    [<Test>] member x.
        ``The Closing Value at Cost Ex should be correct`` () =
            Utils.Round2M (periodItem.ClosingValueCostEx) |> should equal (Utils.Round2M (decimal (gallonsOnHand * 8.) * periodItem.SalesItem.CostPerUnitOfSale))


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
    let salesItem = piSetup.InitialiseDraughtSalesItem ()
    let periodItem = new PeriodItem(salesItem)
    do
        periodItem.ItemsReceived.Add(new ItemReceived(Quantity = 2., InvoicedAmountEx = 212.34M));
        periodItem.ItemsReceived.Add(new ItemReceived(Quantity = 1., InvoicedAmountEx = 109.3M));
        periodItem.ItemsReceived.Add(new ItemReceived(Quantity = 1., InvoicedAmountInc = 120M));

    [<Test>] member x.
        ``The PurchasesTotal amount is correctly calculated`` () =
            periodItem.PurchasesTotal |> should equal (212.34M + 109.3M + (120M/((decimal)(1.0 + periodItem.SalesItem.TaxRate))))


[<TestFixture>]
type ``Given that a PeriodItem has bottles received`` () as this =
    inherit PeriodItemSetup ()
    let periodItem = piSetup.InitialiseBottledSalesItem () |> piSetup.initialisePeriodItem
    let bottlesOnHand = 25.
    let bottlesSold = 23. + (4. * 24.) - bottlesOnHand

    [<Test>] member x.
        ``The ContainersReceived amount is correctly calculated`` () =
            periodItem.ContainersReceived |> should equal (2. + 1. + 1.)

    [<Test>] member x.
        ``The PurchasesEx amount is correctly calculated`` () =
            periodItem.PurchasesEx |> should equal (decimal 4. * periodItem.SalesItem.CostPerContainer)

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
            Utils.Round2M (periodItem.CostOfSalesEx) |> should equal (Utils.Round2M (decimal bottlesSold * periodItem.SalesItem.CostPerUnitOfSale))

    [<Test>] member x.
        ``The SalesPerDay amount is correct`` () = 
            periodItem.SalesPerDay(this.d0, this.d1) |> should equal (bottlesSold / this.dx)

    [<Test>] member x.
        ``The DaysOnHand amount is correct`` () =
            periodItem.DaysOnHand(this.d0, this.d1) |> should equal (25. / (bottlesSold / this.dx) |> int)

    [<Test>] member x.
        ``The Closing Value at Sales Inc should be correct`` () =
            periodItem.ClosingValueSalesInc |> should equal (decimal bottlesOnHand * periodItem.SalesItem.SalesPrice)

    [<Test>] member x.
        ``The Closing Value at Sales Ex should be correct`` () =
            periodItem.ClosingValueSalesEx |> should equal ((decimal bottlesOnHand * periodItem.SalesItem.SalesPrice) / decimal 1.2)

    [<Test>] member x.
        ``The Closing Value at Cost Ex should be correct`` () =
            Utils.Round2M (periodItem.ClosingValueCostEx) |> should equal (Utils.Round2M (decimal bottlesOnHand * periodItem.SalesItem.CostPerUnitOfSale))

[<TestFixture>]
type ``Given that a PeriodItem has snacks received`` () as this =
    inherit PeriodItemSetup ()
    let periodItem = piSetup.InitialiseSnackSalesItem () |> piSetup.initialisePeriodItem
    let packetsOnHand = 25.
    let packetsSold = 23. + (4. * 48.) - packetsOnHand

    [<Test>] member x.
        ``The ContainersReceived amount is correctly calculated`` () =
            periodItem.ContainersReceived |> should equal (2. + 1. + 1.)

    [<Test>] member x.
        ``The PurchasesEx amount is correctly calculated`` () =
            periodItem.PurchasesEx |> should equal (decimal 4. * periodItem.SalesItem.CostPerContainer)

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
            Utils.Round2M (periodItem.CostOfSalesEx) |> should equal (Utils.Round2M (decimal packetsSold * periodItem.SalesItem.CostPerUnitOfSale))

    [<Test>] member x.
        ``The SalesPerDay amount is correct`` () = 
            periodItem.SalesPerDay(this.d0, this.d1) |> should equal (packetsSold / this.dx)

    [<Test>] member x.
        ``The DaysOnHand amount is correct`` () =
            periodItem.DaysOnHand(this.d0, this.d1) |> should equal ((25. / (packetsSold / this.dx)) |> int)

    [<Test>] member x.
        ``The Closing Value at Sales Inc should be correct`` () =
            periodItem.ClosingValueSalesInc |> should equal (decimal packetsOnHand * periodItem.SalesItem.SalesPrice)

    [<Test>] member x.
        ``The Closing Value at Sales Ex should be correct`` () =
            periodItem.ClosingValueSalesEx |> should equal (decimal packetsOnHand * periodItem.SalesItem.SalesPrice)

    [<Test>] member x.
        ``The Closing Value at Cost Ex should be correct`` () =
            Utils.Round2M (periodItem.ClosingValueCostEx) |> should equal (Utils.Round2M (decimal packetsOnHand * periodItem.SalesItem.CostPerUnitOfSale))

[<TestFixture>]
type ``Given that a PeriodItem has spirits received`` () as this =
    inherit PeriodItemSetup ()
    let periodItem = piSetup.InitialiseSpiritSalesItem () |> piSetup.initialisePeriodItem
    let bottlesSold = 0.4 + (4. * 1.) - 0.6
    let measuresPerBottle = 0.7 / 0.035
    let measuresSold = bottlesSold * measuresPerBottle
    let measuresOnHand = 0.6 * measuresPerBottle
    do
        periodItem.OpeningStock <- 0.4
        periodItem.ClosingStock <- 0.6


    [<Test>] member x.
        ``The ContainersReceived amount is correctly calculated`` () =
            periodItem.ContainersReceived |> should equal (2. + 1. + 1.)

    [<Test>] member x.
        ``The PurchasesEx amount is correctly calculated`` () =
            periodItem.PurchasesEx |> should equal (decimal 4. * periodItem.SalesItem.CostPerContainer)

    [<Test>] member x.
        ``The Sales quantity is correct`` () =
            Utils.Round2 periodItem.Sales |> should equal (Utils.Round2 bottlesSold)

    [<Test>] member x.
        ``The SalesInc amount is correct`` () =
            periodItem.SalesInc |> should equal (decimal (measuresSold * 3.5))

    [<Test>] member x.
        ``The SalesEx amount is correct`` () =
            periodItem.SalesEx |> should equal (periodItem.SalesInc / decimal 1.2)

    [<Test>] member x.
        ``The CostOfSales amount is correct`` () =
            Utils.Round2M (periodItem.CostOfSalesEx) |> should equal (Utils.Round2M (decimal periodItem.Sales * periodItem.SalesItem.CostPerContainer))

    [<Test>] member x.
        ``The SalesPerDay amount is correct`` () = 
            Utils.Round4 (periodItem.SalesPerDay(this.d0, this.d1)) |> should equal (Utils.Round4 (bottlesSold / this.dx))

    [<Test>] member x.
        ``The DaysOnHand amount is correct`` () =
            periodItem.DaysOnHand(this.d0, this.d1) |> should equal ((0.6 / (bottlesSold / this.dx)) |> int)

    [<Test>] member x.
        ``The Closing Value at Sales Inc should be correct`` () =
            periodItem.ClosingValueSalesInc |> should equal (decimal measuresOnHand * periodItem.SalesItem.SalesPrice)

    [<Test>] member x.
        ``The Closing Value at Sales Ex should be correct`` () =
            periodItem.ClosingValueSalesEx |> should equal ((decimal measuresOnHand * periodItem.SalesItem.SalesPrice) / decimal 1.2)

    [<Test>] member x.
        ``The Closing Value at Cost Ex should be correct`` () =
            periodItem.ClosingValueCostEx |> should equal (decimal measuresOnHand * periodItem.SalesItem.CostPerUnitOfSale)

[<TestFixture>]
type ``Given that a PeriodItem has wine received`` () as this =
    inherit PeriodItemSetup ()
    let periodItem = piSetup.InitialiseWineSalesItem () |> piSetup.initialisePeriodItem
    let bottlesSold = 23.4 + (4. * 1.) - 13.6
    let measuresPerBottle = 0.75 / 0.175
    let measuresSold = bottlesSold * measuresPerBottle
    let measuresOnHand = 13.6 * measuresPerBottle
    do
        periodItem.OpeningStock <- 23.4
        periodItem.ClosingStock <- 13.6


    [<Test>] member x.
        ``The ContainersReceived amount is correctly calculated`` () =
            periodItem.ContainersReceived |> should equal (2. + 1. + 1.)

    [<Test>] member x.
        ``The PurchasesEx amount is correctly calculated`` () =
            periodItem.PurchasesEx |> should equal (decimal 4. * periodItem.SalesItem.CostPerContainer)

    [<Test>] member x.
        ``The Sales quantity is correct`` () =
            Utils.Round2 periodItem.Sales |> should equal (Utils.Round2 bottlesSold)

    [<Test>] member x.
        ``The SalesInc amount is correct`` () =
            periodItem.SalesInc |> should equal (decimal (measuresSold * 3.45))

    [<Test>] member x.
        ``The SalesEx amount is correct`` () =
            periodItem.SalesEx |> should equal (periodItem.SalesInc / decimal 1.2)

    [<Test>] member x.
        ``The CostOfSales amount is correct`` () =
            Utils.Round2M (periodItem.CostOfSalesEx) |> should equal (Utils.Round2M (decimal periodItem.Sales * periodItem.SalesItem.CostPerContainer))

    [<Test>] member x.
        ``The SalesPerDay amount is correct`` () = 
            Utils.Round4 (periodItem.SalesPerDay(this.d0, this.d1)) |> should equal (Utils.Round4 (bottlesSold / this.dx))

    [<Test>] member x.
        ``The DaysOnHand amount is correct`` () =
            periodItem.DaysOnHand(this.d0, this.d1) |> should equal ((13.6 / (bottlesSold / this.dx)) |> int)

    [<Test>] member x.
        ``The Closing Value at Sales Inc should be correct`` () =
            Utils.Round2M (periodItem.ClosingValueSalesInc) |> should equal (Utils.Round2M (decimal measuresOnHand * periodItem.SalesItem.SalesPrice))

    [<Test>] member x.
        ``The Closing Value at Sales Ex should be correct`` () =
            Utils.Round2M (periodItem.ClosingValueSalesEx) |> should equal (Utils.Round2M ((decimal measuresOnHand * periodItem.SalesItem.SalesPrice) / decimal 1.2))

    [<Test>] member x.
        ``The Closing Value at Cost Ex should be correct`` () =
            Utils.Round2M (periodItem.ClosingValueCostEx) |> should equal (Utils.Round2M (decimal measuresOnHand * periodItem.SalesItem.CostPerUnitOfSale))

[<TestFixture>]
type ``Given that a PeriodItem has no spirits received`` () as this =
    inherit PeriodItemSetup ()
    let periodItem = new StockCheck.Model.PeriodItem( piSetup.InitialiseSpiritSalesItem ())
    let bottlesSold = 0.6
    let measuresPerBottle = 0.7 / 0.035
    let measuresSold = bottlesSold * measuresPerBottle
    let measuresOnHand = 0.3 * measuresPerBottle
    do
        periodItem.OpeningStock <- 0.9
        periodItem.ClosingStock <- 0.3

    [<Test>] member x.
        ``The ContainersReceived amount is correctly calculated`` () =
            periodItem.ContainersReceived |> should equal (0.)

    [<Test>] member x.
        ``The PurchasesEx amount is correctly calculated`` () =
            periodItem.PurchasesEx |> should equal (0.M)

    [<Test>] member x.
        ``The Sales quantity is correct`` () =
            Utils.Round2 periodItem.Sales |> should equal (0.6)

    [<Test>] member x.
        ``The SalesInc amount is correct`` () =
            periodItem.SalesInc |> should equal (decimal (measuresSold * 3.5))

    [<Test>] member x.
        ``The SalesEx amount is correct`` () =
            periodItem.SalesEx |> should equal (periodItem.SalesInc / decimal 1.2)

    [<Test>] member x.
        ``The CostOfSales amount is correct`` () =
            Utils.Round2M (periodItem.CostOfSalesEx) |> should equal (Utils.Round2M (decimal periodItem.Sales * periodItem.SalesItem.CostPerContainer))

    [<Test>] member x.
        ``The SalesPerDay amount is correct`` () = 
            Utils.Round4 (periodItem.SalesPerDay(this.d0, this.d1)) |> should equal (Utils.Round4 (bottlesSold / this.dx))

    [<Test>] member x.
        ``The DaysOnHand amount is correct`` () =
            periodItem.DaysOnHand(this.d0, this.d1) |> should equal ((0.3 / (bottlesSold / this.dx)) |> int)

    [<Test>] member x.
        ``The Closing Value at Sales Inc should be correct`` () =
            periodItem.ClosingValueSalesInc |> should equal (decimal measuresOnHand * periodItem.SalesItem.SalesPrice)

    [<Test>] member x.
        ``The Closing Value at Sales Ex should be correct`` () =
            periodItem.ClosingValueSalesEx |> should equal ((decimal measuresOnHand * periodItem.SalesItem.SalesPrice) / decimal 1.2)

    [<Test>] member x.
        ``The Closing Value at Cost Ex should be correct`` () =
            periodItem.ClosingValueCostEx |> should equal (decimal measuresOnHand * periodItem.SalesItem.CostPerUnitOfSale)
