namespace StockCheck.Model.Test

open System
open NUnit.Framework
open FsUnit
open StockCheck.Model
open StockCheck.Model.Factory
open StockCheck.Model.Conv
open Ploeh.AutoFixture

module Utils =
    let Round2M (x : decimal) =
            Math.Round(x, 2)

    let Round2D (x : decimal<money>) =
            Math.Round(x / 1M<money>, 2)

    let inline Round4 x =
            Math.Round(float x, 4)


module piSetup =
    let NewFixture () =
        new Fixture()

    let InitialiseSalesItem tax ctrSize saleUnit salePrice =
        let salesItem = (NewFixture ()).Create<mySalesItem>()
        { salesItem with TaxRate = tax; ItemName = {salesItem.ItemName with ContainerSize = ctrSize}; SalesUnitType = saleUnit; SalesPrice = salePrice }

    let initialisePeriodItem salesItem =
        let basePI = { defaultPeriodItem with OpeningStock = 23.; ClosingStock = 25. }
        let itemsReceived = 
            [ 
                { myItemReceived.Quantity = 2.; InvoicedAmountEx = 2M * salesItem.CostPerContainer; Id = String.Empty; ReceivedDate = DateTime.Now.Date; InvoicedAmountInc = money 0M }
                { myItemReceived.Quantity = 1.; InvoicedAmountEx = 1M * salesItem.CostPerContainer; Id = String.Empty; ReceivedDate = DateTime.Now.Date; InvoicedAmountInc = money 0M }
                { myItemReceived.Quantity = 1.; InvoicedAmountEx = 1M * salesItem.CostPerContainer; Id = String.Empty; ReceivedDate = DateTime.Now.Date; InvoicedAmountInc = money 0M }
            ]
        { basePI with ItemsReceived = itemsReceived }

    let InitialiseDraughtSalesItem () = InitialiseSalesItem (percentage 0.2) 11. Pint (money 3.25M)

    let InitialiseBottledSalesItem () = InitialiseSalesItem (percentage 0.2) 24. Unit (money 3.60M)

    let InitialiseSnackSalesItem () = InitialiseSalesItem (percentage 0.) 48. Unit (money 0.60M)

    let InitialiseSpiritSalesItem () = InitialiseSalesItem (percentage 0.2) 0.7 Spirit (money 3.5M)

    let InitialiseWineSalesItem () = InitialiseSalesItem (percentage 0.2) 0.75 Wine (money 3.45M)

    let InitialisePostMixSalesItem () =
        let si = InitialiseSalesItem (percentage 0.2) 10. Other (money 1.15M)
        { si with CostPerContainer = (money 29.M); OtherSalesUnit = 17.6056338 }

type PeriodItemSetup () =
    member x.Fixture = piSetup.NewFixture ()
    member x.SalesItem = x.Fixture.Create<mySalesItem>()
    member x.PeriodItem = getPeriodItem x.SalesItem
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
    let itemReceived = this.Fixture.Create<myItemReceived>()
    let salesItem = piSetup.InitialiseDraughtSalesItem ()
    let basePeriodItem = getPeriodItem salesItem
    let periodItem = {basePeriodItem with ItemsReceived = [ itemReceived ]}

    [<Test>] member x.
        ``The goods are added to the items received collection`` () =
            periodItem.ItemsReceived |> should haveCount 1

[<TestFixture>]
type ``Given that a PeriodItem is being copied for the next period`` () as this =
    inherit PeriodItemSetup ()
    let periodItem1 = this.Fixture.Create<myPeriodItem>()
    let periodItem2 = copyForNextPeriod periodItem1

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
    let periodItemInfo = getPeriodItemInfo (periodItem, periodItem.ItemsReceived, (getSalesItemInfo periodItem.SalesItem))
    let gallonsOnHand = LanguagePrimitives.FloatWithMeasure<gal> 25.
    let pintsOnHand = Conv.convertGallonsToPints gallonsOnHand
    let gallonsSold = LanguagePrimitives.FloatWithMeasure<gal> (23. + (4. * 11.)) - gallonsOnHand
    let pintsSold = 8. * gallonsSold

    [<Test>] member x.
        ``The ContainersReceived amount is correctly calculated`` () =
            periodItemInfo.ContainersReceived |> should equal (2. + 1. + 1.)

    [<Test>] member x.
        ``The PurchasesEx amount is correctly calculated`` () =
            periodItemInfo.PurchasesEx |> should equal (decimal 4. * periodItem.SalesItem.CostPerContainer)

    [<Test>] member x.
        ``The Sales quantity is correct`` () =
            periodItemInfo.Sales |> should equal gallonsSold

    [<Test>] member x.
        ``The SalesInc amount is correct`` () =
            periodItemInfo.SalesInc |> should equal (decimal(pintsSold) * 3.25M)

    [<Test>] member x.
        ``The SalesEx amount is correct`` () =
            periodItemInfo.SalesEx |> should equal ((decimal(pintsSold) * 3.25M) / decimal 1.2)

    [<Test>] member x.
        ``The CostOfSales amount is correct`` () =
            Utils.Round2D (periodItemInfo.CostOfSalesEx) |> should equal (Utils.Round2D ((decimal pintsSold) * (getSalesItemInfo periodItem.SalesItem).CostPerUnitOfSale))

//    [<Test>] member x.
//        ``The SalesPerDay amount is correct`` () = 
//            periodItemInfo.SalesPerDay(this.d0, this.d1) |> should equal (gallonsSold / this.dx)
//
//    [<Test>] member x.
//        ``The DaysOnHand amount is correct`` () =
//            periodItemInfo.DaysOnHand(this.d0, this.d1) |> should equal ((gallonsOnHand / (gallonsSold / this.dx)) |> int)

    [<Test>] member x.
        ``The Profit is correct`` () =
            periodItemInfo.MarkUp |> should equal (periodItemInfo.SalesEx - periodItemInfo.CostOfSalesEx)

    [<Test>] member x.
        ``The Closing Value at Sales Inc should be correct`` () =
            periodItemInfo.ClosingValueSalesInc |> should equal (decimal (pintsOnHand) * periodItem.SalesItem.SalesPrice)

    [<Test>] member x.
        ``The Closing Value at Sales Ex should be correct`` () =
            periodItemInfo.ClosingValueSalesEx |> should equal ((decimal (pintsOnHand) * periodItem.SalesItem.SalesPrice) / decimal 1.2)

    [<Test>] member x.
        ``The Closing Value at Cost Ex should be correct`` () =
            Utils.Round2D (periodItemInfo.ClosingValueCostEx) |> should equal (Utils.Round2D (decimal (pintsOnHand) * (getSalesItemInfo periodItem.SalesItem).CostPerUnitOfSale))


[<TestFixture>]
type ``Given that a PeriodItem has Tax inclusive draught items received`` () as this =
    inherit PeriodItemSetup ()
    let basePI = this.Fixture.Create<mySalesItem> () |> piSetup.initialisePeriodItem
    let itemsReceived = 
        [ 
            { myItemReceived.Quantity = 2.; InvoicedAmountEx = 0M<money>; Id = String.Empty; ReceivedDate = DateTime.Now.Date; InvoicedAmountInc = money 212.34M }
            { myItemReceived.Quantity = 1.; InvoicedAmountEx = 0M<money>; Id = String.Empty; ReceivedDate = DateTime.Now.Date; InvoicedAmountInc = money 109.3M }
            { myItemReceived.Quantity = 1.; InvoicedAmountEx = 0M<money>; Id = String.Empty; ReceivedDate = DateTime.Now.Date; InvoicedAmountInc = money 109.3M }
        ]
    let periodItem = { basePI with ItemsReceived = itemsReceived }
    let periodItemInfo = getPeriodItemInfo (periodItem, periodItem.ItemsReceived, (getSalesItemInfo periodItem.SalesItem))

    [<Test>] member x.
        ``The PurchasesInc amount is correctly calculated`` () =
            periodItemInfo.PurchasesInc |> should equal (212.34M + 109.3M + 109.3M)

[<TestFixture>]
type ``Given that a PeriodItem has mixed Tax Exclusive and inclusive draught items received`` () =
    inherit PeriodItemSetup ()
    let salesItem = piSetup.InitialiseDraughtSalesItem ()
    let basePI = getPeriodItem salesItem
    let itemsReceived = 
        [ 
            { myItemReceived.Quantity = 2.; InvoicedAmountEx = 212.34M<money>; Id = String.Empty; ReceivedDate = DateTime.Now.Date; InvoicedAmountInc = money 0M }
            { myItemReceived.Quantity = 1.; InvoicedAmountEx = 109.3M<money>; Id = String.Empty; ReceivedDate = DateTime.Now.Date; InvoicedAmountInc = money 0M }
            { myItemReceived.Quantity = 1.; InvoicedAmountEx = 120M<money>; Id = String.Empty; ReceivedDate = DateTime.Now.Date; InvoicedAmountInc = money 0M }
        ]
    let periodItem = { basePI with ItemsReceived = itemsReceived }
    let periodItemInfo = getPeriodItemInfo (periodItem, periodItem.ItemsReceived, (getSalesItemInfo periodItem.SalesItem))

    [<Test>] member x.
        ``The PurchasesTotal amount is correctly calculated`` () =
            periodItemInfo.PurchasesTotal |> should equal (212.34M + 109.3M + (120M/((decimal)(1.0 + (periodItem.SalesItem.TaxRate / 1.0<percentage>)))))


[<TestFixture>]
type ``Given that a PeriodItem has bottles received`` () as this =
    inherit PeriodItemSetup ()
    let periodItem = piSetup.InitialiseBottledSalesItem () |> piSetup.initialisePeriodItem
    let periodItemInfo = getPeriodItemInfo (periodItem, periodItem.ItemsReceived, (getSalesItemInfo periodItem.SalesItem))
    let bottlesOnHand = 25.
    let bottlesSold = 23. + (4. * 24.) - bottlesOnHand

    [<Test>] member x.
        ``The ContainersReceived amount is correctly calculated`` () =
            periodItemInfo.ContainersReceived |> should equal (2. + 1. + 1.)

    [<Test>] member x.
        ``The PurchasesEx amount is correctly calculated`` () =
            periodItemInfo.PurchasesEx |> should equal (decimal 4. * periodItem.SalesItem.CostPerContainer)

    [<Test>] member x.
        ``The Sales quantity is correct`` () =
            periodItemInfo.Sales |> should equal bottlesSold

    [<Test>] member x.
        ``The SalesInc amount is correct`` () =
            periodItemInfo.SalesInc |> should equal (decimal(bottlesSold) * 3.60M)

    [<Test>] member x.
        ``The SalesEx amount is correct`` () =
            periodItemInfo.SalesEx |> should equal ((decimal(bottlesSold) * 3.60M) / decimal 1.2)

    [<Test>] member x.
        ``The CostOfSales amount is correct`` () =
            Utils.Round2D (periodItemInfo.CostOfSalesEx) |> should equal (Utils.Round2D (decimal bottlesSold * (getSalesItemInfo periodItem.SalesItem).CostPerUnitOfSale))

//    [<Test>] member x.
//        ``The SalesPerDay amount is correct`` () = 
//            periodItem.SalesPerDay(this.d0, this.d1) |> should equal (bottlesSold / this.dx)
//
//    [<Test>] member x.
//        ``The DaysOnHand amount is correct`` () =
//            periodItem.DaysOnHand(this.d0, this.d1) |> should equal (25. / (bottlesSold / this.dx) |> int)

    [<Test>] member x.
        ``The Closing Value at Sales Inc should be correct`` () =
            periodItemInfo.ClosingValueSalesInc |> should equal (decimal bottlesOnHand * periodItem.SalesItem.SalesPrice)

    [<Test>] member x.
        ``The Closing Value at Sales Ex should be correct`` () =
            periodItemInfo.ClosingValueSalesEx |> should equal ((decimal bottlesOnHand * periodItem.SalesItem.SalesPrice) / decimal 1.2)

    [<Test>] member x.
        ``The Closing Value at Cost Ex should be correct`` () =
            Utils.Round2D (periodItemInfo.ClosingValueCostEx) |> should equal (Utils.Round2D (decimal bottlesOnHand * (getSalesItemInfo periodItem.SalesItem).CostPerUnitOfSale))

[<TestFixture>]
type ``Given that a PeriodItem has snacks received`` () as this =
    inherit PeriodItemSetup ()
    let periodItem = piSetup.InitialiseSnackSalesItem () |> piSetup.initialisePeriodItem
    let salesItemInfo = getSalesItemInfo periodItem.SalesItem
    let periodItemInfo = getPeriodItemInfo (periodItem, periodItem.ItemsReceived, salesItemInfo)
    let packetsOnHand = 25.
    let packetsSold = 23. + (4. * 48.) - packetsOnHand

    [<Test>] member x.
        ``The ContainersReceived amount is correctly calculated`` () =
            periodItemInfo.ContainersReceived |> should equal (2. + 1. + 1.)

    [<Test>] member x.
        ``The PurchasesEx amount is correctly calculated`` () =
            periodItemInfo.PurchasesEx |> should equal (decimal 4. * periodItem.SalesItem.CostPerContainer)

    [<Test>] member x.
        ``The Sales quantity is correct`` () =
            periodItemInfo.Sales |> should equal packetsSold

    [<Test>] member x.
        ``The SalesInc amount is correct`` () =
            periodItemInfo.SalesInc |> should equal (decimal(packetsSold) * 0.60M)

    [<Test>] member x.
        ``The SalesEx amount is correct`` () =
            periodItemInfo.SalesEx |> should equal ((decimal(packetsSold) * 0.60M) / decimal 1.0)

    [<Test>] member x.
        ``The CostOfSales amount is correct`` () =
            Utils.Round2D (periodItemInfo.CostOfSalesEx) |> should equal (Utils.Round2D (decimal packetsSold * salesItemInfo.CostPerUnitOfSale))

//    [<Test>] member x.
//        ``The SalesPerDay amount is correct`` () = 
//            periodItem.SalesPerDay(this.d0, this.d1) |> should equal (packetsSold / this.dx)
//
//    [<Test>] member x.
//        ``The DaysOnHand amount is correct`` () =
//            periodItem.DaysOnHand(this.d0, this.d1) |> should equal ((25. / (packetsSold / this.dx)) |> int)

    [<Test>] member x.
        ``The Closing Value at Sales Inc should be correct`` () =
            periodItemInfo.ClosingValueSalesInc |> should equal (decimal packetsOnHand * periodItem.SalesItem.SalesPrice)

    [<Test>] member x.
        ``The Closing Value at Sales Ex should be correct`` () =
            periodItemInfo.ClosingValueSalesEx |> should equal (decimal packetsOnHand * periodItem.SalesItem.SalesPrice)

    [<Test>] member x.
        ``The Closing Value at Cost Ex should be correct`` () =
            Utils.Round2D (periodItemInfo.ClosingValueCostEx) |> should equal (Utils.Round2D (decimal packetsOnHand * salesItemInfo.CostPerUnitOfSale))

[<TestFixture>]
type ``Given that a PeriodItem has spirits received`` () as this =
    inherit PeriodItemSetup ()
    let baseItem = piSetup.InitialiseSpiritSalesItem () |> piSetup.initialisePeriodItem
    let periodItem = { baseItem with OpeningStock = 0.4; ClosingStock = 0.6 }
    let salesItemInfo = getSalesItemInfo periodItem.SalesItem
    let periodItemInfo = getPeriodItemInfo (periodItem, periodItem.ItemsReceived, salesItemInfo)
    let bottlesSold = 0.4 + (4. * 1.) - 0.6
    let measuresPerBottle = 0.7 / 0.035
    let measuresSold = bottlesSold * measuresPerBottle
    let measuresOnHand = 0.6 * measuresPerBottle

    [<Test>] member x.
        ``The ContainersReceived amount is correctly calculated`` () =
            periodItemInfo.ContainersReceived |> should equal (2. + 1. + 1.)

    [<Test>] member x.
        ``The PurchasesEx amount is correctly calculated`` () =
            periodItemInfo.PurchasesEx |> should equal (decimal 4. * periodItem.SalesItem.CostPerContainer)

    [<Test>] member x.
        ``The Sales quantity is correct`` () =
            Utils.Round2 periodItemInfo.Sales |> should equal (Utils.Round2 bottlesSold)

    [<Test>] member x.
        ``The SalesInc amount is correct`` () =
            periodItemInfo.SalesInc |> should equal (decimal (measuresSold * 3.5))

    [<Test>] member x.
        ``The SalesEx amount is correct`` () =
            periodItemInfo.SalesEx |> should equal (periodItemInfo.SalesInc / decimal 1.2)

    [<Test>] member x.
        ``The CostOfSales amount is correct`` () =
            Utils.Round2D (periodItemInfo.CostOfSalesEx) |> should equal (Utils.Round2D (decimal periodItemInfo.Sales * periodItem.SalesItem.CostPerContainer))

//    [<Test>] member x.
//        ``The SalesPerDay amount is correct`` () = 
//            Utils.Round4 (periodItem.SalesPerDay(this.d0, this.d1)) |> should equal (Utils.Round4 (bottlesSold / this.dx))
//
//    [<Test>] member x.
//        ``The DaysOnHand amount is correct`` () =
//            periodItem.DaysOnHand(this.d0, this.d1) |> should equal ((0.6 / (bottlesSold / this.dx)) |> int)

    [<Test>] member x.
        ``The Closing Value at Sales Inc should be correct`` () =
            periodItemInfo.ClosingValueSalesInc |> should equal (decimal measuresOnHand * periodItem.SalesItem.SalesPrice)

    [<Test>] member x.
        ``The Closing Value at Sales Ex should be correct`` () =
            periodItemInfo.ClosingValueSalesEx |> should equal ((decimal measuresOnHand * periodItem.SalesItem.SalesPrice) / decimal 1.2)

    [<Test>] member x.
        ``The Closing Value at Cost Ex should be correct`` () =
            periodItemInfo.ClosingValueCostEx |> should equal (decimal measuresOnHand * salesItemInfo.CostPerUnitOfSale)

[<TestFixture>]
type ``Given that a PeriodItem has wine received`` () as this =
    inherit PeriodItemSetup ()
    let baseItem = piSetup.InitialiseWineSalesItem () |> piSetup.initialisePeriodItem
    let periodItem = { baseItem with OpeningStock = 23.4; ClosingStock = 13.6 }
    let salesItemInfo = getSalesItemInfo periodItem.SalesItem
    let periodItemInfo = getPeriodItemInfo (periodItem, periodItem.ItemsReceived, salesItemInfo)
    let bottlesSold = 23.4 + (4. * 1.) - 13.6
    let measuresPerBottle = 0.75 / 0.175
    let measuresSold = bottlesSold * measuresPerBottle
    let measuresOnHand = 13.6 * measuresPerBottle


    [<Test>] member x.
        ``The ContainersReceived amount is correctly calculated`` () =
            periodItemInfo.ContainersReceived |> should equal (2. + 1. + 1.)

    [<Test>] member x.
        ``The PurchasesEx amount is correctly calculated`` () =
            periodItemInfo.PurchasesEx |> should equal (decimal 4. * periodItem.SalesItem.CostPerContainer)

    [<Test>] member x.
        ``The Sales quantity is correct`` () =
            Utils.Round2 periodItemInfo.Sales |> should equal (Utils.Round2 bottlesSold)

    [<Test>] member x.
        ``The SalesInc amount is correct`` () =
            periodItemInfo.SalesInc |> should equal (decimal (measuresSold * 3.45))

    [<Test>] member x.
        ``The SalesEx amount is correct`` () =
            periodItemInfo.SalesEx |> should equal (periodItemInfo.SalesInc / decimal 1.2)

    [<Test>] member x.
        ``The CostOfSales amount is correct`` () =
            Utils.Round2D (periodItemInfo.CostOfSalesEx) |> should equal (Utils.Round2D (decimal periodItemInfo.Sales * periodItem.SalesItem.CostPerContainer))

//    [<Test>] member x.
//        ``The SalesPerDay amount is correct`` () = 
//            Utils.Round4 (periodItem.SalesPerDay(this.d0, this.d1)) |> should equal (Utils.Round4 (bottlesSold / this.dx))
//
//    [<Test>] member x.
//        ``The DaysOnHand amount is correct`` () =
//            periodItem.DaysOnHand(this.d0, this.d1) |> should equal ((13.6 / (bottlesSold / this.dx)) |> int)

    [<Test>] member x.
        ``The Closing Value at Sales Inc should be correct`` () =
            Utils.Round2D (periodItemInfo.ClosingValueSalesInc) |> should equal (Utils.Round2D (decimal measuresOnHand * periodItem.SalesItem.SalesPrice))

    [<Test>] member x.
        ``The Closing Value at Sales Ex should be correct`` () =
            Utils.Round2D (periodItemInfo.ClosingValueSalesEx) |> should equal (Utils.Round2D ((decimal measuresOnHand * periodItem.SalesItem.SalesPrice) / decimal 1.2))

    [<Test>] member x.
        ``The Closing Value at Cost Ex should be correct`` () =
            Utils.Round2D (periodItemInfo.ClosingValueCostEx) |> should equal (Utils.Round2D (decimal measuresOnHand * salesItemInfo.CostPerUnitOfSale))

[<TestFixture>]
type ``Given that a PeriodItem has no spirits received`` () as this =
    inherit PeriodItemSetup ()
    let periodItem = getPeriodItem (piSetup.InitialiseSpiritSalesItem ())
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

[<TestFixture>]
type ``Given that a PeriodItem has no PostMix received`` () as this =
    inherit PeriodItemSetup ()
    let periodItem = new StockCheck.Model.PeriodItem( piSetup.InitialisePostMixSalesItem ())
    let boxesSold = 0.6
    let measuresPerLitre = 17.6056338
    let measuresSold = boxesSold * 10. * measuresPerLitre
    let measuresOnHand = 0.3 * 10. * measuresPerLitre
    do
        periodItem.OpeningStock <- 0.9
        periodItem.ClosingStock <- 0.3


    [<Test>] member x.
        ``The SalesInc amount is correct`` () =
            periodItem.SalesInc |> should equal (decimal measuresSold * periodItem.SalesItem.SalesPrice)

    [<Test>] member x.
        ``The SalesEx amount is correct`` () =
            periodItem.SalesEx |> should equal (periodItem.SalesInc / decimal 1.2)

    [<Test>] member x.
        ``The CostOfSales amount is correct`` () =
            Utils.Round2M (periodItem.CostOfSalesEx) |> should equal (Utils.Round2M (decimal periodItem.Sales * periodItem.SalesItem.CostPerContainer))

    [<Test>] member x.
        ``The SalesPerDay amount is correct`` () = 
            Utils.Round4 (periodItem.SalesPerDay(this.d0, this.d1)) |> should equal (Utils.Round4 (boxesSold / this.dx))

    [<Test>] member x.
        ``The DaysOnHand amount is correct`` () =
            periodItem.DaysOnHand(this.d0, this.d1) |> should equal ((0.3 / (boxesSold / this.dx)) |> int)

    [<Test>] member x.
        ``The Closing Value at Sales Inc should be correct`` () =
            periodItem.ClosingValueSalesInc |> should equal (decimal measuresOnHand * periodItem.SalesItem.SalesPrice)

    [<Test>] member x.
        ``The Closing Value at Sales Ex should be correct`` () =
            periodItem.ClosingValueSalesEx |> should equal ((decimal measuresOnHand * periodItem.SalesItem.SalesPrice) / decimal 1.2)

    [<Test>] member x.
        ``The Closing Value at Cost Ex should be correct`` () =
            Utils.Round2M periodItem.ClosingValueCostEx |> should equal (Utils.Round2M (decimal periodItem.ClosingStock * periodItem.SalesItem.CostPerContainer))

[<TestFixture>]
type ``Given that a PeriodItem has no Draught received`` () as this =
    inherit PeriodItemSetup ()
    let periodItem = new StockCheck.Model.PeriodItem( piSetup.InitialiseDraughtSalesItem ())
    let gallonsOnHand = LanguagePrimitives.FloatWithMeasure<gal> 23.
    let pintsOnHand = Conv.convertGallonsToPints gallonsOnHand
    let gallonsSold = 2.
    let pintsSold = 8. * gallonsSold 
    do
        periodItem.OpeningStock <- 25.
        periodItem.ClosingStock <- float gallonsOnHand

    [<Test>] member x.
        ``The SalesInc amount is correct`` () =
            periodItem.SalesInc |> should equal (decimal pintsSold * periodItem.SalesItem.SalesPrice)

    [<Test>] member x.
        ``The SalesEx amount is correct`` () =
            periodItem.SalesEx |> should equal (periodItem.SalesInc / decimal 1.2)

    [<Test>] member x.
        ``The CostOfSales amount is correct`` () =
            Utils.Round2M (periodItem.CostOfSalesEx) |> should equal (Utils.Round2M (decimal periodItem.Sales * (periodItem.SalesItem.CostPerContainer / decimal periodItem.SalesItem.ContainerSize)))

    [<Test>] member x.
        ``The SalesPerDay amount is correct`` () = 
            Utils.Round4 (periodItem.SalesPerDay(this.d0, this.d1)) |> should equal (Utils.Round4 (gallonsSold / this.dx))

    [<Test>] member x.
        ``The DaysOnHand amount is correct`` () =
            periodItem.DaysOnHand(this.d0, this.d1) |> should equal ((gallonsOnHand / (gallonsSold / this.dx)) |> int)

    [<Test>] member x.
        ``The Closing Value at Sales Inc should be correct`` () =
            periodItem.ClosingValueSalesInc |> should equal (decimal pintsOnHand * periodItem.SalesItem.SalesPrice)

    [<Test>] member x.
        ``The Closing Value at Sales Ex should be correct`` () =
            periodItem.ClosingValueSalesEx |> should equal ((decimal pintsOnHand * periodItem.SalesItem.SalesPrice) / decimal 1.2)

    [<Test>] member x.
        ``The Closing Value at Cost Ex should be correct`` () =
            Utils.Round2M periodItem.ClosingValueCostEx |> should equal (Utils.Round2M (decimal periodItem.ClosingStock * (periodItem.SalesItem.CostPerContainer / decimal periodItem.SalesItem.ContainerSize)))