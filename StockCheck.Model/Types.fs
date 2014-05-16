namespace StockCheck.Model

open System
open System.Collections.Generic

module Utils =
    let MarkUp sale cost =
        sale - cost

    let GrossProfit sale cost =
        match sale with
        | dsale when dsale = (decimal 0) -> 0.
        | _ -> float ((MarkUp sale cost)/sale)

    let LessTax rate price = 
        price / (decimal 1 + decimal rate)

    let ValueOfQuantity qty unit ppUnit =
        decimal(qty * unit * float ppUnit)

type SalesItem() = 
    let costPerUnitOfSale (costPerContainer: decimal) (containerSize : float) (salesUnitsPerContainerUnit: float) : decimal =
        match costPerContainer with
            | 0M -> decimal 0
            | _ -> decimal (float costPerContainer/(containerSize * salesUnitsPerContainerUnit))

    member val Id = String.Empty with get, set
    member val ContainerSize = 0. with get, set
    member val CostPerContainer = decimal 0 with get, set
    member val LedgerCode = String.Empty with get, set
    member val Name = String.Empty with get, set
    member val SalesPrice = decimal 0 with get, set
    member val TaxRate = 0. with get, set
    member val UllagePerContainer = 0 with get, set
    member val SalesUnitsPerContainerUnit = 0. with get, set
    member this.MarkUp = Utils.MarkUp this.SalesPrice this.CostPerUnitOfSale
    member this.CostPerUnitOfSale = costPerUnitOfSale this.CostPerContainer this.ContainerSize this.SalesUnitsPerContainerUnit
    member this.IdealGP = Utils.GrossProfit this.SalesPrice this.CostPerUnitOfSale

type ItemReceived() =
    member val Id = String.Empty with get, set
    member val Quantity = 0. with get, set
    member val ReceivedDate = DateTime.MinValue with get, set
    member val InvoicedAmountEx = decimal 0 with get, set
    member val InvoicedAmountInc = decimal 0 with get, set
    member val Reference = String.Empty with get, set

type PeriodItem(salesItem : SalesItem) = 
    let itemsReceived = List<ItemReceived>()
    let lessTax = Utils.LessTax salesItem.TaxRate

    member val OpeningStock = 0. with get, set
    member val ClosingStock = 0. with get, set
    member val SalesItem = salesItem
    member this.ItemsReceived = itemsReceived;
    member this.ReceiveItems receivedDate quantity invoiceAmountEx invoiceAmountInc reference =
            let item = ItemReceived()
            item.Quantity <- quantity
            item.ReceivedDate <- receivedDate
            item.InvoicedAmountEx <- invoiceAmountEx
            item.InvoicedAmountInc <- invoiceAmountInc
            item.Reference <- reference
            this.ItemsReceived.Add(item)
    member this.CopyForNextPeriod () =
            let periodItem = PeriodItem (salesItem)
            periodItem.OpeningStock <- this.ClosingStock
            periodItem.ClosingStock <- 0.
            periodItem
    member this.ContainersReceived = itemsReceived |> Seq.sumBy (fun i -> i.Quantity)
    member this.TotalUnits = this.ContainersReceived * salesItem.ContainerSize
    member this.Sales = this.OpeningStock + this.TotalUnits - this.ClosingStock
    member this.ContainersSold = this.Sales / salesItem.ContainerSize
    member this.PurchasesEx = itemsReceived |> Seq.sumBy (fun i -> i.InvoicedAmountEx)
    member this.PurchasesInc = itemsReceived |> Seq.sumBy (fun i -> i.InvoicedAmountInc)
    member this.PurchasesTotal = this.PurchasesEx + lessTax this.PurchasesInc
    member this.SalesInc = Utils.ValueOfQuantity this.Sales salesItem.SalesUnitsPerContainerUnit salesItem.SalesPrice
    member this.SalesEx = lessTax this.SalesInc
    member this.CostOfSalesEx = decimal this.ContainersSold * salesItem.CostPerContainer
    member this.Profit = salesItem.MarkUp * decimal (salesItem.SalesUnitsPerContainerUnit * this.Sales)
    member this.SalesPerDay (startDate: DateTime, endDate: DateTime) = this.Sales / float (endDate.Subtract(startDate).Days + 1)
    member this.DaysOnHand (startDate: DateTime, endDate: DateTime) = this.ClosingStock / this.SalesPerDay(startDate, endDate) |> int
    member this.Ullage = this.ContainersSold * (float salesItem.UllagePerContainer)
    member this.UllageAtSale = (decimal this.Ullage) * salesItem.SalesPrice
    member this.ClosingValueCostEx = Utils.ValueOfQuantity this.ClosingStock 1. (salesItem.CostPerContainer/decimal salesItem.ContainerSize)
    member this.ClosingValueSalesInc = Utils.ValueOfQuantity this.ClosingStock salesItem.SalesUnitsPerContainerUnit salesItem.SalesPrice
    member this.ClosingValueSalesEx = lessTax this.ClosingValueSalesInc

type Period() =
    member val Id = String.Empty with get, set
    member val Name = String.Empty with get, set 
    member val EndOfPeriod = DateTime.MinValue with get, set
    member val StartOfPeriod = DateTime.MinValue with get, set
    member val Items = List<PeriodItem>() with get, set
    member val ClosingValueCostEx = decimal 0 with get
    member val ClosingValueSalesInc = 0 with get
    member val ClosingValueSalesEx = 0 with get
    static member private CloseToStart (item: PeriodItem) =
            item.OpeningStock <- item.ClosingStock
            item.ClosingStock <- 0.
    static member private InitialiseFrom (source: Period) =
            let period = Period()
            period.StartOfPeriod <- source.EndOfPeriod.AddDays(1.)
            period.EndOfPeriod <- period.StartOfPeriod
            period
    static member InitialiseFromClone source =
            let period = Period.InitialiseFrom source
            period.Items.AddRange(source.Items |> Seq.map(fun i -> i.CopyForNextPeriod()))
            period
    static member InitialiseWithoutZeroCarriedItems source =
            let period = Period.InitialiseFrom source
            let items = source.Items |> Seq.filter (fun i -> i.OpeningStock > 0. && i.ClosingStock > 0.)
            period.Items.AddRange(items)
            period.Items |> Seq.iter (fun i -> Period.CloseToStart i)
            period

type InvoiceLine(salesItem : SalesItem) =
    member val Id = String.Empty with get, set
    member val SalesItem = salesItem with get, set
    member val Quantity = 0. with get, set
    member val InvoicedAmountEx = decimal 0. with get, set
    member val InvoicedAmountInc = decimal 0. with get, set

type Invoice() =
    member val Id = String.Empty with get, set
    member val Supplier = String.Empty with get, set
    member val InvoiceNumber = String.Empty with get, set
    member val InvoiceDate = DateTime.MinValue with get, set
    member val DeliveryDate = DateTime.MinValue with get, set
    member val InvoiceLines = List<InvoiceLine>() with get, set
