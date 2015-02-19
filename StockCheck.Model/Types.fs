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
    with
        member this.toString() =
            match this with
            | Pint -> "Pint"
            | Unit -> "Unit"
            | Spirit -> "Spirit"
            | Fortified -> "Fortified"
            | Wine -> "Wine"
            | Other -> "Other"
        static member fromString s =
            match s with
            | "Pint" -> Pint
            | "Unit" -> Unit
            | "Spirit" -> Spirit
            | "Fortified" -> Fortified
            | "Wine" -> Wine
            | "Other" -> Other
            | _ -> Unit


module Utils =
    let MarkUp sale cost =
        sale - cost

    let GrossProfit sale cost =
        match sale with
        | 0.M -> 0.
        | _ -> float ((MarkUp sale cost)/sale)

    let LessTax rate price = 
        price / decimal(1. + rate)

    let private ValueOfQuantity qty unit size ppUnit =
        decimal (qty * unit * size * float ppUnit)

    let ValueOfQuantityT t unit size qty ppUnit =
        match t with
        | Pint | Unit -> ValueOfQuantity qty unit 1.0 ppUnit
        | Spirit | Fortified | Wine | Other -> ValueOfQuantity qty unit size ppUnit

    let inline Round2 x =
            Math.Round(float x, 2)

type SalesItem() = 
    let costPerUnitOfSale costPerContainer containerSize salesUnitsPerContainerUnit =
        match costPerContainer with
        | 0M -> decimal 0
        | _ -> decimal (float costPerContainer/(containerSize * salesUnitsPerContainerUnit))

    member private this.SalesPriceEx = Utils.LessTax this.TaxRate this.SalesPrice
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
    member this.MarkUp = Utils.MarkUp this.SalesPriceEx this.CostPerUnitOfSale
    member this.CostPerUnitOfSale = costPerUnitOfSale this.CostPerContainer this.ContainerSize this.SalesUnitsPerContainerUnit    
    member this.IdealGP = Utils.GrossProfit this.SalesPriceEx this.CostPerUnitOfSale
    member this.SalesUnitsPerContainerUnit = 
        match this.SalesUnitType with
        | Pint -> float Conv.ptPerGal
        | Unit -> 1.0
        | Spirit -> float Conv.shotsPerLtr
        | Fortified -> float Conv.doublesPerLtr
        | Wine -> float Conv.wineGlassPerLtr
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

    member private this.ValueOfQuantity = Utils.ValueOfQuantityT salesItem.SalesUnitType salesItem.SalesUnitsPerContainerUnit salesItem.ContainerSize 
    member private this.ContRec = if this.ContainersReceived > 0. then Some(this.ContainersReceived) else None
    member val Id = String.Empty with get, set
    member val OpeningStock = 0. with get, set
    member val ClosingStockExpr = String.Empty with get, set
    member val ClosingStock = 0. with get, set
    member val SalesItem = salesItem
    member this.ItemsReceived = itemsReceived;
    member this.ReceiveItems receivedDate quantity invoiceAmountEx invoiceAmountInc =
            let item = ItemReceived(Quantity = quantity, ReceivedDate = receivedDate, InvoicedAmountEx = invoiceAmountEx, InvoicedAmountInc = invoiceAmountInc)
            this.ItemsReceived.Add(item)
    member this.CopyForNextPeriod () =
            PeriodItem (salesItem, OpeningStock = this.ClosingStock, ClosingStock = 0.)

    member this.ContainersReceived = itemsReceived |> Seq.sumBy (fun i -> i.Quantity)
    member this.TotalUnits = 
        match this.SalesItem.SalesUnitType with
        | Pint | Unit | Other -> this.ContainersReceived * salesItem.ContainerSize
        | Spirit | Fortified | Wine -> this.ContainersReceived
    member this.Sales = Utils.Round2 (this.OpeningStock + this.TotalUnits - this.ClosingStock)
    member this.ContainersSold = this.Sales / salesItem.ContainerSize
    member this.PurchasesEx = itemsReceived |> Seq.sumBy (fun i -> i.InvoicedAmountEx)
    member this.PurchasesInc = itemsReceived |> Seq.sumBy (fun i -> i.InvoicedAmountInc)
    member this.PurchasesTotal = this.PurchasesEx + lessTax this.PurchasesInc
    member this.SalesInc = this.ValueOfQuantity this.Sales salesItem.SalesPrice
    member this.SalesEx = this.SalesInc |> lessTax
    member private this.SalesCost = 
        let c = decimal this.Sales * salesItem.CostPerContainer
        match this.SalesItem.SalesUnitType with
        | salesUnitType.Spirit | salesUnitType.Fortified | salesUnitType.Other ->
            c
        | _ ->
            c / decimal this.SalesItem.ContainerSize
    member this.CostOfSalesEx = 
        match this.ContRec with
        | Some(c) -> 
            decimal ((float (this.PurchasesTotal) / this.TotalUnits) * this.Sales)
        | None -> 
            this.SalesCost
    
    member this.Profit = this.SalesEx - this.CostOfSalesEx
    member this.SalesPerDay (startDate: DateTime, endDate: DateTime) = this.Sales / float (endDate.Subtract(startDate).Days + 1)
    member this.DaysOnHand (startDate: DateTime, endDate: DateTime) = this.ClosingStock / this.SalesPerDay(startDate, endDate) |> int
    member this.Ullage = this.ContainersSold * (float salesItem.UllagePerContainer)
    member this.UllageAtSale = (decimal this.Ullage) * salesItem.SalesPrice
    member this.ClosingValueCostEx = this.ValueOfQuantity this.ClosingStock salesItem.CostPerUnitOfSale
    member this.ClosingValueSalesInc = this.ValueOfQuantity this.ClosingStock salesItem.SalesPrice
    member this.ClosingValueSalesEx = this.ClosingValueSalesInc |> lessTax

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
            let items = source.Items |> Seq.filter (fun i -> i.OpeningStock > 0. && i.ClosingStock > 0.) |> period.Items.AddRange
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
