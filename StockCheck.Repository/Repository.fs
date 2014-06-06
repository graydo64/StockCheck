namespace StockCheck.Repository

open System
open System.Collections.Generic
open System.Linq
open System.ComponentModel.DataAnnotations
open Raven.Client
open Raven.Client.Document

[<CLIMutable>]
type SalesItem = 
    { Id : string
      ContainerSize : float
      CostPerContainer : decimal
      LedgerCode : string
      Name : string
      SalesPrice : decimal
      TaxRate : float
      UllagePerContainer : int
      SalesUnitType : string
      OtherSalesUnit : float }

[<CLIMutable>]
type ItemReceived = 
    { SalesItemId : string
      Quantity : float
      ReceivedDate : DateTime
      InvoicedAmountEx : decimal
      InvoicedAmountInc : decimal }

[<CLIMutable>]
type PeriodItem = 
    { SalesItem : SalesItem
      OpeningStock : float
      ClosingStockExpr : string
      ClosingStock : float
      ItemsReceived : ItemReceived seq }

[<CLIMutable>]
type Period = 
    { Id : string
      Name : string
      StartOfPeriod : DateTime
      EndOfPeriod : DateTime
      Items : PeriodItem seq }

[<CLIMutable>]
type InvoiceLine = 
    { Id : string
      SalesItem : SalesItem
      Quantity : float
      InvoicedAmountEx : decimal
      InvoicedAmountInc : decimal }

[<CLIMutable>]
type Invoice = 
    { Id : string
      Supplier : string
      InvoiceNumber : string
      InvoiceDate : DateTime
      DeliveryDate : DateTime
      InvoiceLines : InvoiceLine seq }

[<CLIMutable>]
type Supplier = 
    { Id : string
      Name : string }

module internal MapToModel = 
    let irMap (ir : ItemReceived) = 
        StockCheck.Model.ItemReceived
            (Quantity = ir.Quantity, ReceivedDate = ir.ReceivedDate, InvoicedAmountEx = decimal ir.InvoicedAmountEx, 
             InvoicedAmountInc = decimal ir.InvoicedAmountInc)
    let siMap (si : SalesItem) = 
        match si with
        | i when String.IsNullOrEmpty(i.Id) -> StockCheck.Model.SalesItem()
        | _ -> StockCheck.Model.SalesItem
                (Id = si.Id.ToString(), ContainerSize = si.ContainerSize, CostPerContainer = si.CostPerContainer, 
                    LedgerCode = si.LedgerCode, Name = si.Name, SalesPrice = si.SalesPrice, TaxRate = si.TaxRate, 
                    UllagePerContainer = si.UllagePerContainer, SalesUnitType = StockCheck.Model.Converters.ToSalesUnitType si.SalesUnitType,
                    OtherSalesUnit = si.OtherSalesUnit)
    
    let piMap (pi : PeriodItem) = 
        let modelItem = StockCheck.Model.PeriodItem(siMap pi.SalesItem)
        modelItem.OpeningStock <- pi.OpeningStock
        modelItem.ClosingStockExpr <- pi.ClosingStockExpr
        modelItem.ClosingStock <- pi.ClosingStock
        modelItem
    
    let pMap (p : Period) = 
        let items = p.Items |> Seq.map piMap
        StockCheck.Model.Period
            (Id = p.Id.ToString(), Name = p.Name, StartOfPeriod = p.StartOfPeriod, EndOfPeriod = p.EndOfPeriod,
             Items = List<StockCheck.Model.PeriodItem> items)
    
    let ilMap (il : InvoiceLine) = 
        let salesItem = siMap il.SalesItem
        let modelItem = StockCheck.Model.InvoiceLine(salesItem)
        modelItem.Id <- il.Id.ToString()
        modelItem.Quantity <- il.Quantity
        modelItem.InvoicedAmountEx <- il.InvoicedAmountEx
        modelItem.InvoicedAmountInc <- il.InvoicedAmountInc
        modelItem
    
    let iMap (i : Invoice) = 
        let lines = i.InvoiceLines |> Seq.map ilMap
        StockCheck.Model.Invoice
            (Id = i.Id.ToString(), Supplier = i.Supplier, InvoiceNumber = i.InvoiceNumber, InvoiceDate = i.InvoiceDate, 
             DeliveryDate = i.DeliveryDate, InvoiceLines = List<StockCheck.Model.InvoiceLine>(lines))
    
    let supMap (s : Supplier) = 
        let supplier = StockCheck.Model.Supplier()
        supplier.Id <- s.Id.ToString()
        supplier.Name <- s.Name
        supplier

type Query(documentStore : IDocumentStore) = 

    let dbName = "StockCheck"
    
    let getItemReceived dt il = 
        { ItemReceived.SalesItemId = il.SalesItem.Id
          InvoicedAmountEx = il.InvoicedAmountEx
          InvoicedAmountInc = il.InvoicedAmountInc
          Quantity = il.Quantity
          ReceivedDate = dt }
        
    member internal this.GetSuppliers() = 
        use session = documentStore.OpenSession(dbName)
        session.Query<Supplier>().ToList()
    
    member internal this.GetSalesItem name ledgerCode = 
        use session = documentStore.OpenSession dbName
        session.Query<SalesItem>().Where(fun i -> i.Name = name && i.LedgerCode = ledgerCode).FirstOrDefault()
    
    member internal this.GetSalesItemById (id : string) = 
        use session = documentStore.OpenSession(dbName)
        session.Load<SalesItem>(id)
    
    member internal this.GetPeriod (id : string) = 
        use session = documentStore.OpenSession(dbName)
        session.Load<Period>(id)
    
    member internal this.GetPeriodByName name = 
        use session = documentStore.OpenSession dbName
        session.Query<Period>().Where(fun i -> i.Name = name).FirstOrDefault()
    
    member internal this.GetPeriods () = 
        use session = documentStore.OpenSession(dbName)
        session.Query<Period>().ToList()
    
    member internal this.GetSalesItems () = 
        use session = documentStore.OpenSession(dbName)
        session.Query<SalesItem>().Take(1024).ToList()
    
    member internal this.GetInvoice (id : string) = 
        use session = documentStore.OpenSession(dbName)
        session.Load<Invoice>(id)
    
    member internal this.GetInvoices () = 
        use session = documentStore.OpenSession(dbName)
        session.Query<Invoice>().ToList()
    
    member internal this.GetInvoicesByDateRange startDate endDate = 
        use session = documentStore.OpenSession(dbName)
        session.Query<Invoice>().Where(fun i -> i.DeliveryDate >= startDate && i.DeliveryDate <= endDate).ToList()
    
    member this.GetModelSalesItemById id = this.GetSalesItemById id |> MapToModel.siMap
    
    member this.GetModelPeriodById id = 
        let p = this.GetPeriod id
        let invoices = this.GetInvoicesByDateRange p.StartOfPeriod p.EndOfPeriod
        let projectAsItemReceived dt il = il |> Seq.map (fun i -> getItemReceived dt i)
        let itemsReceived = invoices |> Seq.collect(fun i -> projectAsItemReceived i.DeliveryDate i.InvoiceLines)
        
        let getPeriodItem (salesItem : SalesItem) = 
            let items = p.Items.Where(fun i -> i.SalesItem.Id = salesItem.Id)
            match items.Any() with
            | true -> items.First()
            | _ -> 
                { PeriodItem.SalesItem = salesItem
                  OpeningStock = 0.
                  ClosingStockExpr = String.Empty
                  ClosingStock = 0.
                  ItemsReceived = [] }
        
        let invoicePeriodItems = 
                invoices
                |> Seq.collect (fun i -> i.InvoiceLines |> Seq.map (fun il -> il.SalesItem))
                |> Seq.distinct
                |> Seq.map getPeriodItem

        let periodItems = Seq.concat [invoicePeriodItems; p.Items] |> Seq.distinct

        let modelPeriod = MapToModel.pMap p
        modelPeriod.Items.Clear()
        modelPeriod.Items.AddRange(periodItems |> Seq.map MapToModel.piMap)
        modelPeriod.Items
        |> Seq.iter (fun mpi -> 
               itemsReceived.Where(fun r -> r.SalesItemId = mpi.SalesItem.Id)
               |> Seq.iter 
                      (fun ir -> mpi.ReceiveItems ir.ReceivedDate ir.Quantity ir.InvoicedAmountEx ir.InvoicedAmountInc)
               |> ignore)
        |> ignore
        modelPeriod
    
    member this.GetModelPeriods = this.GetPeriods() |> Seq.map MapToModel.pMap
    member this.GetModelSalesItems = this.GetSalesItems() |> Seq.map MapToModel.siMap
    member this.GetModelInvoice id = this.GetInvoice id |> MapToModel.iMap
    member this.GetModelInvoices() = this.GetInvoices() |> Seq.map MapToModel.iMap
    member this.GetModelInvoicesByDateRange start finish = this.GetInvoicesByDateRange start finish |> Seq.map MapToModel.iMap
    member this.GetModelSuppliers = this.GetSuppliers() |> Seq.map MapToModel.supMap

module internal MapFromModel = 
    let idMap id = match id with
                    | x when String.IsNullOrEmpty(x) -> Guid.NewGuid().ToString()
                    | _ -> id
        
    let piMap (documentStore) (pi : StockCheck.Model.PeriodItem) = 
        let query = new Query(documentStore)
        { SalesItem = query.GetSalesItem pi.SalesItem.Name pi.SalesItem.LedgerCode
          OpeningStock = pi.OpeningStock
          ClosingStockExpr = pi.ClosingStockExpr
          ClosingStock = pi.ClosingStock
          ItemsReceived = List<ItemReceived>() }
    
    let siMap (si : StockCheck.Model.SalesItem) = 
        { Id = idMap si.Id
          ContainerSize = si.ContainerSize
          CostPerContainer = si.CostPerContainer
          LedgerCode = si.LedgerCode
          Name = si.Name
          SalesPrice = si.SalesPrice
          TaxRate = si.TaxRate
          UllagePerContainer = si.UllagePerContainer
          SalesUnitType = StockCheck.Model.Converters.ToSalesUnitTypeString si.SalesUnitType
          OtherSalesUnit = si.OtherSalesUnit }
    
    let pMap (documentStore) (p : StockCheck.Model.Period) = 
        { Id = idMap p.Id
          Name = p.Name
          StartOfPeriod = p.StartOfPeriod
          EndOfPeriod = p.EndOfPeriod
          Items = 
              p.Items
              |> Seq.map (fun i -> piMap documentStore i)
              |> List.ofSeq }
    
    let ilMap (il : StockCheck.Model.InvoiceLine) = 
        { Id = idMap il.Id
          SalesItem = siMap il.SalesItem
          Quantity = il.Quantity
          InvoicedAmountEx = il.InvoicedAmountEx
          InvoicedAmountInc = il.InvoicedAmountInc }
    
    let iMap (i : StockCheck.Model.Invoice) = 
        { Id = idMap i.Id
          Supplier = i.Supplier
          InvoiceNumber = i.InvoiceNumber
          InvoiceDate = i.InvoiceDate
          DeliveryDate = i.DeliveryDate
          InvoiceLines = i.InvoiceLines |> Seq.map ilMap }

type Persister(documentStore : IDocumentStore) = 

    let periodMap = MapFromModel.pMap documentStore

    let saveDocument d =
        use session = documentStore.OpenSession("StockCheck")
        session.Store(d)
        session.SaveChanges()

    member this.Save(si : StockCheck.Model.SalesItem) = 
        MapFromModel.siMap si |> saveDocument
    
    member this.Save(p : StockCheck.Model.Period) = 
        periodMap p |> saveDocument
    
    member this.Save(i : StockCheck.Model.Invoice) = 
        MapFromModel.iMap i |> saveDocument
    
    member this.Save(s : StockCheck.Model.Supplier) = 
        let supplier = 
            { Supplier.Id = MapFromModel.idMap s.Id
              Name = s.Name }
        supplier |> saveDocument
