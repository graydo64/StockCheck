namespace StockCheck.ModelFs

open System
open System.Collections.Generic

type SalesItem() = 
    let gp (sale: decimal, cost: decimal) = 
        if sale = decimal 0 
        then decimal 0 
        else (sale - cost)/sale
    let costPerUnitOfSale (costPerContainer: decimal, containerSize, unitOfSale) = 
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
    member this.CostPerUnitOfSale = costPerUnitOfSale(this.CostPerContainer, this.ContainerSize, this.UnitOfSale);
    member this.IdealGP = gp(this.SalesPrice, this.CostPerUnitOfSale)

type ItemReceived = { Quantity: int;
    ReceivedDate : DateTime;
    InvoicedAmountEx : decimal;
    InvoicedAmountInc : decimal;
}

type PeriodItem(salesItem : SalesItem) = 
    let itemsReceived = new List<ItemReceived>()
    member val OpeningStock = 0 with get, set
    member val ClosingStock = 0 with get, set
    member val SalesItem = salesItem
    member this.ItemsReceived = itemsReceived;
    member this.ReceiveItems receivedDate quantity invoiceAmountEx invoiceAmountInc =
            this.ItemsReceived.Add({Quantity = quantity; ReceivedDate = receivedDate; InvoicedAmountEx = invoiceAmountEx; InvoicedAmountInc = invoiceAmountInc})
    member this.CopyForNextPeriod() =
            let periodItem = new PeriodItem(salesItem)
            periodItem.OpeningStock <- this.ClosingStock
            periodItem


type Period() = 
    member val EndOfPeriod = DateTime.MinValue with get, set
    member val StartOfPeriod = DateTime.MinValue with get, set
    member val Items = new List<PeriodItem>() with get, set
    static member InitialiseFromClone(source : Period): Period =
            let period = new Period()
            period.StartOfPeriod <- source.EndOfPeriod.AddDays(1.)
            period
    static member InitialiseWithoutZeroCarriedItems(source : Period): Period =
            let period = new Period()
            period.StartOfPeriod <- source.EndOfPeriod.AddDays(1.)
            period


