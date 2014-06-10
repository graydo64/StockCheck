namespace StockCheck.Model

open System
open System.Collections.Generic

type salesUnitType =
    | Pint
    | Unit
    | Spirit
    | Fortified
    | Wine
    | Other

[<Measure>] type l
[<Measure>] type shot
[<Measure>] type double
[<Measure>] type gal
[<Measure>] type pt

module Utils =
    let MarkUp sale cost =
        sale - cost

    let GrossProfit sale cost =
        match sale with
        | dsale when dsale = (decimal 0) -> 0.
        | _ -> float ((MarkUp sale cost)/sale)

    let LessTax rate price = 
        price / (decimal 1 + decimal rate)

    let private ValueOfQuantity qty unit ppUnit =
        decimal (qty * unit * float ppUnit)

    let private ValueOfQuantity2 qty unit size ppUnit =
        decimal (qty * unit * size * float ppUnit)

    let ValueOfQuantityT (t : salesUnitType) qty unit size ppUnit =
        match t with
        | Pint -> ValueOfQuantity qty unit ppUnit
        | Unit -> ValueOfQuantity qty unit ppUnit
        | Spirit -> ValueOfQuantity2 qty unit size ppUnit
        | Fortified -> ValueOfQuantity2 qty unit size ppUnit
        | Wine -> ValueOfQuantity2 qty unit size ppUnit
        | Other -> ValueOfQuantity qty unit ppUnit

    let Round2 x =
            Math.Round(x * 100.)/100.

    let Round2M (x : decimal) =
            Math.Round(x * 100.M)/100.M

    let Round4 x =
            Math.Round(x * 10000.)/10000.

    let ptPerGal : float<pt/gal> = 8.<pt/gal>

    let shotsPerLtr : float<shot/l> = 28.5714<shot/l>

    let convertLitresToShots (x : float<l>) = x * shotsPerLtr
    let convertGallonsToPints (x : float<gal>) = x * ptPerGal

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
    member val SalesUnitType = salesUnitType.Unit with get, set
    member val OtherSalesUnit = 0. with get, set
    member val UllagePerContainer = 0 with get, set
    member this.MarkUp = Utils.MarkUp (Utils.LessTax this.TaxRate this.SalesPrice) this.CostPerUnitOfSale
    member this.CostPerUnitOfSale = costPerUnitOfSale this.CostPerContainer this.ContainerSize this.SalesUnitsPerContainerUnit    
    member this.IdealGP = Utils.GrossProfit (Utils.LessTax this.TaxRate this.SalesPrice) this.CostPerUnitOfSale
    member this.SalesUnitsPerContainerUnit = 
        match this.SalesUnitType with
        | Pint -> 8.
        | Unit -> 1.0
        | Spirit -> 1.0/0.035
        | Fortified -> 1.0/0.05
        | Wine -> 1.0/0.175
        | Other -> this.OtherSalesUnit

type ItemReceived() =
    member val Id = String.Empty with get, set
    member val Quantity = 0. with get, set
    member val ReceivedDate = DateTime.MinValue with get, set
    member val InvoicedAmountEx = decimal 0 with get, set
    member val InvoicedAmountInc = decimal 0 with get, set

type PeriodItem(salesItem : SalesItem) = 
    let itemsReceived = List<ItemReceived>()
    let lessTax = Utils.LessTax salesItem.TaxRate

    member val OpeningStock = 0. with get, set
    member val ClosingStockExpr = String.Empty with get, set
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
            periodItem.ClosingStock <- 0.
            periodItem
    member this.ContainersReceived = itemsReceived |> Seq.sumBy (fun i -> i.Quantity)
    member this.TotalUnits = 
        match this.SalesItem.SalesUnitType with
        | Pint -> this.ContainersReceived * salesItem.ContainerSize
        | Unit -> this.ContainersReceived * salesItem.ContainerSize
        | Spirit -> this.ContainersReceived
        | Fortified -> this.ContainersReceived
        | Wine -> this.ContainersReceived
        | Other -> this.ContainersReceived * salesItem.ContainerSize
    member this.Sales = Utils.Round2 (this.OpeningStock + this.TotalUnits - this.ClosingStock)
    member this.ContainersSold = this.Sales / salesItem.ContainerSize
    member this.PurchasesEx = itemsReceived |> Seq.sumBy (fun i -> i.InvoicedAmountEx)
    member this.PurchasesInc = itemsReceived |> Seq.sumBy (fun i -> i.InvoicedAmountInc)
    member this.PurchasesTotal = this.PurchasesEx + lessTax this.PurchasesInc
    member this.SalesInc = Utils.ValueOfQuantityT salesItem.SalesUnitType this.Sales salesItem.SalesUnitsPerContainerUnit salesItem.ContainerSize salesItem.SalesPrice
    member this.SalesEx = lessTax this.SalesInc
    member this.CostOfSalesEx = 
        if this.ContainersReceived > 0. 
        then
            decimal ((float (this.PurchasesTotal) / this.TotalUnits) * this.Sales)
        else
            if this.SalesItem.SalesUnitType = salesUnitType.Spirit || this.SalesItem.SalesUnitType = salesUnitType.Fortified then
                decimal this.Sales * this.SalesItem.CostPerContainer
            else
                decimal (this.Sales / this.SalesItem.ContainerSize) * this.SalesItem.CostPerContainer
    
    member this.Profit = salesItem.MarkUp * decimal (salesItem.SalesUnitsPerContainerUnit * this.Sales)
    member this.SalesPerDay (startDate: DateTime, endDate: DateTime) = this.Sales / float (endDate.Subtract(startDate).Days + 1)
    member this.DaysOnHand (startDate: DateTime, endDate: DateTime) = this.ClosingStock / this.SalesPerDay(startDate, endDate) |> int
    member this.Ullage = this.ContainersSold * (float salesItem.UllagePerContainer)
    member this.UllageAtSale = (decimal this.Ullage) * salesItem.SalesPrice
    member this.ClosingValueCostEx = Utils.ValueOfQuantityT salesItem.SalesUnitType this.ClosingStock salesItem.SalesUnitsPerContainerUnit salesItem.ContainerSize salesItem.CostPerUnitOfSale
    member this.ClosingValueSalesInc = Utils.ValueOfQuantityT salesItem.SalesUnitType this.ClosingStock salesItem.SalesUnitsPerContainerUnit salesItem.ContainerSize salesItem.SalesPrice
    member this.ClosingValueSalesEx = lessTax this.ClosingValueSalesInc

type Period() =
    member val Id = String.Empty with get, set
    member val Name = String.Empty with get, set 
    member val EndOfPeriod = DateTime.MinValue with get, set
    member val StartOfPeriod = DateTime.MinValue with get, set
    member val Items = List<PeriodItem>() with get, set
    member this.SalesEx = this.Items |> Seq.sumBy(fun i -> i.SalesEx)
    member this.ClosingValueSalesInc = this.Items |> Seq.sumBy(fun i -> i.ClosingValueSalesInc)
    member this.ClosingValueSalesEx = this.Items |> Seq.sumBy(fun i -> i.ClosingValueSalesEx)
    member this.ClosingValueCostEx = this.Items |> Seq.sumBy(fun i -> i.ClosingValueCostEx)
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

type Supplier() =
    member val Id = String.Empty with get, set
    member val Name = String.Empty with get, set

module Converters =

    let ToSalesUnitTypeString t = 
        match t with
        | Pint -> "Pint"
        | Unit -> "Unit"
        | Spirit -> "Spirit"
        | Fortified -> "Fortified"
        | Wine -> "Wine"
        | Other -> "Other"

    let ToSalesUnitType t = 
        match t with
        | "Pint" -> Pint
        | "Unit" -> Unit
        | "Spirit" -> Spirit
        | "Fortified" -> Fortified
        | "Wine" -> Wine
        | "Other" -> Other
        | _ -> Unit
