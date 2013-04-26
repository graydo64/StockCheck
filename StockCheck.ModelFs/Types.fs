namespace StockCheck.ModelFs

open System
open System.Collections.Generic

module Utils =
    let GrossProfit sale cost =
        if sale = decimal 0
        then decimal 0
        else (sale - cost)/sale

    let LessTax price rate = 
        price / (decimal 1 + rate)

type SalesItem() = 
    let costPerUnitOfSale (costPerContainer: decimal) (containerSize: float) (unitOfSale: float) : decimal = 
        if costPerContainer = decimal 0 
        then decimal 0 
        else decimal ((float costPerContainer)/(containerSize/unitOfSale))
    member val ContainerSize = 0. with get, set
    member val CostPerContainer = decimal 0 with get, set
    member val LedgerCode = String.Empty with get, set
    member val Name = String.Empty with get, set
    member val SalesPrice = decimal 0 with get, set
    member val TaxRate = 0. with get, set
    member val UllagePerContainer = 0 with get, set
    member val UnitOfSale = 0. with get, set
    member this.CostPerUnitOfSale = costPerUnitOfSale this.CostPerContainer this.ContainerSize this.UnitOfSale
    member this.IdealGP = Utils.GrossProfit this.SalesPrice this.CostPerUnitOfSale

type ItemReceived() =
    member val Quantity = 0. with get, set
    member val ReceivedDate = DateTime.MinValue with get, set
    member val InvoicedAmountEx = decimal 0 with get, set
    member val InvoicedAmountInc = decimal 0 with get, set

type PeriodItem(salesItem : SalesItem) = 
    let itemsReceived = List<ItemReceived>()

    member val OpeningStock = 0. with get, set
    member val ClosingStock = 0. with get, set
    member val SalesItem = salesItem
    member this.ItemsReceived = itemsReceived;
    member this.ReceiveItems receivedDate quantity invoiceAmountEx invoiceAmountInc =
            let item = ItemReceived()
            item.Quantity <- quantity
            item.ReceivedDate <- receivedDate
            item.InvoicedAmountEx <- invoiceAmountEx
            item.InvoicedAmountInc <- invoiceAmountInc
            this.ItemsReceived.Add(item)
    member this.CopyForNextPeriod () =
            let periodItem = PeriodItem (salesItem)
            periodItem.OpeningStock <- this.ClosingStock
            periodItem
    member this.ContainersReceived = itemsReceived |> Seq.sumBy (fun i -> i.Quantity)
    member this.TotalUnits = float this.ContainersReceived * salesItem.ContainerSize
    member this.Sales = this.OpeningStock + this.TotalUnits - this.ClosingStock
    member this.ContainersSold = this.Sales / salesItem.ContainerSize
    member this.PurchasesEx = itemsReceived |> Seq.sumBy (fun i -> i.InvoicedAmountEx)
    member this.PurchasesInc = itemsReceived |> Seq.sumBy (fun i -> i.InvoicedAmountInc)
    member this.PurchasesTotal = this.PurchasesEx + Utils.LessTax this.PurchasesInc (decimal salesItem.TaxRate)
    member this.SalesInc = decimal (this.Sales / salesItem.UnitOfSale * float salesItem.SalesPrice)
    member this.SalesEx = Utils.LessTax (decimal this.SalesInc) (decimal salesItem.TaxRate)
    member this.CostOfSalesEx = decimal this.ContainersSold * salesItem.CostPerContainer
    member val SalesPerDay = 0 with get
    member val DaysOnHand = 0 with get
    member this.Ullage = this.ContainersSold * (float salesItem.UllagePerContainer)
    member this.UllageAtSale = (decimal this.Ullage) * salesItem.SalesPrice
    member this.ClosingValueCostEx = decimal (this.ClosingStock / salesItem.ContainerSize) * salesItem.CostPerContainer
    member this.ClosingValueSalesInc = decimal (this.ClosingStock / salesItem.UnitOfSale) * salesItem.SalesPrice
    member this.ClosingValueSalesEx = Utils.LessTax this.ClosingValueSalesInc (decimal salesItem.TaxRate)

type Period() = 
    member val EndOfPeriod = DateTime.MinValue with get, set
    member val StartOfPeriod = DateTime.MinValue with get, set
    member val Items = List<PeriodItem>() with get, set
    member val ClosingValueCostEx = 0 with get
    member val ClosingValueSalesInc = 0 with get
    member val ClosingValueSalesEx = 0 with get
    static member private InitialiseFrom (source: Period) =
            let period = Period()
            period.StartOfPeriod <- source.EndOfPeriod.AddDays(1.)
            period
    static member InitialiseFromClone source =
            let period = Period.InitialiseFrom source
            period.Items.AddRange(source.Items)
            period
    static member InitialiseWithoutZeroCarriedItems source =
            let period = Period.InitialiseFrom source
            let items = source.Items |> Seq.filter (fun i -> i.OpeningStock > 0. && i.ClosingStock > 0.)
            period.Items.AddRange(items)
            period


