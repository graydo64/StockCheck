namespace StockCheck.Repository

open System
open System.Collections.Generic
open System.Linq
open MongoDB.Bson
open MongoDB.Driver.Builders
open System.ComponentModel.DataAnnotations

[<CLIMutable>]
type SalesItem = 
    { _id : ObjectId
      ContainerSize : float
      CostPerContainer : decimal
      LedgerCode : string
      Name : string
      SalesPrice : decimal
      TaxRate : float
      UllagePerContainer : int
      SalesUnitsPerContainerUnit : float }

[<CLIMutable>]
type ItemReceived = 
    { SalesItemId : ObjectId
      Quantity : float
      ReceivedDate : DateTime
      InvoicedAmountEx : decimal
      InvoicedAmountInc : decimal }

[<CLIMutable>]
type PeriodItem = 
    { SalesItem : SalesItem
      OpeningStock : float
      ClosingStock : float
      ItemsReceived : ItemReceived seq }

[<CLIMutable>]
type Period = 
    { _id : ObjectId
      Name : string
      StartOfPeriod : DateTime
      EndOfPeriod : DateTime
      Items : PeriodItem seq }

[<CLIMutable>]
type InvoiceLine = 
    { _id : ObjectId
      SalesItem : SalesItem
      Quantity : float
      InvoicedAmountEx : decimal
      InvoicedAmountInc : decimal }

[<CLIMutable>]
type Invoice = 
    { _id : ObjectId
      Supplier : string
      InvoiceNumber : string
      InvoiceDate : DateTime
      DeliveryDate : DateTime
      InvoiceLines : InvoiceLine seq }

[<CLIMutable>]
type Supplier = 
    { _id : ObjectId
      Name : string }

module internal MapToModel = 
    let irMap (ir : ItemReceived) = 
        StockCheck.Model.ItemReceived
            (Quantity = ir.Quantity, ReceivedDate = ir.ReceivedDate, InvoicedAmountEx = decimal ir.InvoicedAmountEx, 
             InvoicedAmountInc = decimal ir.InvoicedAmountInc)
    let siMap (si : SalesItem) = 
        StockCheck.Model.SalesItem
            (Id = si._id.ToString(), ContainerSize = si.ContainerSize, CostPerContainer = si.CostPerContainer, 
             LedgerCode = si.LedgerCode, Name = si.Name, SalesPrice = si.SalesPrice, TaxRate = si.TaxRate, 
             UllagePerContainer = si.UllagePerContainer, SalesUnitsPerContainerUnit = si.SalesUnitsPerContainerUnit)
    
    let piMap (pi : PeriodItem) = 
        let modelItem = StockCheck.Model.PeriodItem(siMap pi.SalesItem)
        modelItem.OpeningStock <- pi.OpeningStock
        modelItem.ClosingStock <- pi.ClosingStock
        modelItem
    
    let pMap (p : Period) = 
        let items = p.Items |> Seq.map piMap
        StockCheck.Model.Period
            (Id = p._id.ToString(), Name = p.Name, StartOfPeriod = p.StartOfPeriod, EndOfPeriod = p.EndOfPeriod, 
             Items = List<StockCheck.Model.PeriodItem> items)
    
    let ilMap (il : InvoiceLine) = 
        let salesItem = siMap il.SalesItem
        let modelItem = StockCheck.Model.InvoiceLine(salesItem)
        modelItem.Id <- il._id.ToString()
        modelItem.Quantity <- il.Quantity
        modelItem.InvoicedAmountEx <- il.InvoicedAmountEx
        modelItem.InvoicedAmountInc <- il.InvoicedAmountInc
        modelItem
    
    let iMap (i : Invoice) = 
        let lines = i.InvoiceLines |> Seq.map ilMap
        StockCheck.Model.Invoice
            (Id = i._id.ToString(), Supplier = i.Supplier, InvoiceNumber = i.InvoiceNumber, InvoiceDate = i.InvoiceDate, 
             DeliveryDate = i.DeliveryDate, InvoiceLines = List<StockCheck.Model.InvoiceLine>(lines))
    
    let supMap (s : Supplier) = 
        let supplier = StockCheck.Model.Supplier()
        supplier.Id <- s._id.ToString()
        supplier.Name <- s.Name
        supplier

type Query(connectionString : string) = 
    let db = createMongoServerWithConnString (connectionString) |> getMongoDatabase "StockCheck"
    
    let getItemReceived (dt : DateTime) (il : InvoiceLine) = 
        { ItemReceived.SalesItemId = il.SalesItem._id
          InvoicedAmountEx = il.InvoicedAmountEx
          InvoicedAmountInc = il.InvoicedAmountInc
          Quantity = il.Quantity
          ReceivedDate = dt }
    
    let getModelSalesItem (si : SalesItem) = MapToModel.siMap si
    
    member internal this.GetSuppliers = 
        let collection = db |> getMongoCollection<Supplier> "Supplier"
        collection.FindAll().ToList<Supplier>()
    
    member internal this.GetSalesItem (name : string) (ledgerCode : string) = 
        let collection = db |> getMongoCollection<SalesItem> "SalesItem"
        let value = BsonValue.Create(name)
        collection.FindOne(Query.EQ("Name", value))
    
    member internal this.GetSalesItemById(id : string) = 
        let collection = db |> getMongoCollection<SalesItem> "SalesItem"
        let value = BsonValue.Create(ObjectId.Parse(id))
        collection.FindOne(Query.EQ("_id", value))
    
    member internal this.GetPeriod(id : string) = 
        let collection = db |> getMongoCollection<Period> "Period"
        let value = BsonValue.Create(ObjectId.Parse(id))
        collection.FindOne(Query.EQ("_id", value))
    
    member internal this.GetPeriodByName(name : string) = 
        let collection = db |> getMongoCollection<Period> "Period"
        let value = BsonValue.Create(name)
        collection.FindOne(Query.EQ("Name", value))
    
    member internal this.GetPeriods = 
        let collection = db |> getMongoCollection<Period> "Period"
        collection.FindAll().ToList<Period>()
    
    member internal this.GetSalesItems = 
        let collection = db |> getMongoCollection<SalesItem> "SalesItem"
        collection.FindAll().ToList<SalesItem>()
    
    member internal this.GetInvoice(id : string) = 
        let collection = db |> getMongoCollection<Invoice> "Invoice"
        let invoiceId = BsonValue.Create(ObjectId.Parse(id))
        collection.FindOne(Query.EQ("_id", invoiceId))
    
    member internal this.GetInvoices = 
        let collection = db |> getMongoCollection<Invoice> "Invoice"
        collection.FindAll().ToList<Invoice>()
    
    member internal this.GetInvoicesByDateRange startDate endDate = 
        let collection = db |> getMongoCollection<Invoice> "Invoice"
        let query = 
            Query.And
                (Query.GTE("DeliveryDate", BsonValue.Create(startDate)), 
                 Query.LTE("DeliveryDate", BsonValue.Create(endDate)))
        collection.Find(query).ToList<Invoice>()
    
    member this.GetModelSalesItemById(id : string) = this.GetSalesItemById id |> MapToModel.siMap
    member this.GetModelSalesItem (name : string) (ledgerCode : string) = 
        this.GetSalesItem name ledgerCode |> MapToModel.siMap
    member this.GetModelPeriod period = MapToModel.pMap period
    
    member this.GetModelPeriodById id = 
        let p = this.GetPeriod id
        let invoices = this.GetInvoicesByDateRange p.StartOfPeriod p.EndOfPeriod
        let project (dt : DateTime) (il : InvoiceLine seq) = il |> Seq.map (fun i -> getItemReceived dt i)
        let getItemsReceived (invoice : Invoice) = project invoice.DeliveryDate invoice.InvoiceLines
        
        let res = 
            seq { 
                for invoice in invoices do
                    yield! getItemsReceived invoice
            }
        
        let modelPeriod = MapToModel.pMap p
        
        let getPeriodItem (salesItem : SalesItem) = 
            let items = p.Items.Where(fun i -> i.SalesItem._id = salesItem._id)
            match items.Any() with
            | true -> items.First()
            | _ -> 
                { PeriodItem.SalesItem = salesItem
                  OpeningStock = 0.
                  ClosingStock = 0.
                  ItemsReceived = [] }
        
        let salesItemsList = 
            invoices
            |> Seq.collect (fun i -> i.InvoiceLines |> Seq.map (fun il -> il.SalesItem))
            |> Seq.distinct
        
        let periodItems = salesItemsList |> Seq.map getPeriodItem
        modelPeriod.Items.Clear()
        modelPeriod.Items.AddRange(periodItems |> Seq.map MapToModel.piMap)
        let collectInvoiceLines (salesItemId : string) = res.Where(fun r -> r.SalesItemId = ObjectId.Parse(salesItemId))
        modelPeriod.Items
        |> Seq.iter (fun mpi -> 
               collectInvoiceLines mpi.SalesItem.Id
               |> Seq.iter 
                      (fun ir -> mpi.ReceiveItems ir.ReceivedDate ir.Quantity ir.InvoicedAmountEx ir.InvoicedAmountInc)
               |> ignore)
        |> ignore
        modelPeriod
    
    member this.GetModelPeriods = this.GetPeriods |> Seq.map this.GetModelPeriod
    member this.GetModelSalesItems = this.GetSalesItems |> Seq.map getModelSalesItem
    member this.GetModelInvoice(id : string) = this.GetInvoice id |> MapToModel.iMap
    member this.GetModelInvoices = this.GetInvoices |> Seq.map MapToModel.iMap
    member this.GetModelSuppliers = this.GetSuppliers |> Seq.map MapToModel.supMap

module internal MapFromModel = 
    let idMap id = 
        match id with
        | i when String.IsNullOrEmpty(i) -> ObjectId.GenerateNewId()
        | _ -> ObjectId.Parse(id)
    
    let irMap (ir : StockCheck.Model.ItemReceived) = 
        { SalesItemId = ObjectId.Parse(ir.Id)
          Quantity = ir.Quantity
          ReceivedDate = ir.ReceivedDate
          InvoicedAmountEx = ir.InvoicedAmountEx
          InvoicedAmountInc = ir.InvoicedAmountInc }
    
    let piMap (connectionString : string) (pi : StockCheck.Model.PeriodItem) = 
        let query = new Query(connectionString)
        { SalesItem = query.GetSalesItem pi.SalesItem.Name pi.SalesItem.LedgerCode
          OpeningStock = pi.OpeningStock
          ClosingStock = pi.ClosingStock
          ItemsReceived = List<ItemReceived>() }
    
    let siMap (si : StockCheck.Model.SalesItem) = 
        { _id = idMap si.Id
          ContainerSize = si.ContainerSize
          CostPerContainer = si.CostPerContainer
          LedgerCode = si.LedgerCode
          Name = si.Name
          SalesPrice = si.SalesPrice
          TaxRate = si.TaxRate
          UllagePerContainer = si.UllagePerContainer
          SalesUnitsPerContainerUnit = si.SalesUnitsPerContainerUnit }
    
    let pMap (connectionString : string) (p : StockCheck.Model.Period) = 
        { _id = idMap p.Id
          Name = p.Name
          StartOfPeriod = p.StartOfPeriod
          EndOfPeriod = p.EndOfPeriod
          Items = 
              p.Items
              |> Seq.map (fun i -> piMap connectionString i)
              |> List.ofSeq }
    
    let ilMap (il : StockCheck.Model.InvoiceLine) = 
        { _id = idMap il.Id
          SalesItem = siMap il.SalesItem
          Quantity = il.Quantity
          InvoicedAmountEx = il.InvoicedAmountEx
          InvoicedAmountInc = il.InvoicedAmountInc }
    
    let iMap (i : StockCheck.Model.Invoice) = 
        { _id = idMap i.Id
          Supplier = i.Supplier
          InvoiceNumber = i.InvoiceNumber
          InvoiceDate = i.InvoiceDate
          DeliveryDate = i.DeliveryDate
          InvoiceLines = i.InvoiceLines |> Seq.map ilMap }

type Persister(connectionString : string) = 
    let db = createMongoServerWithConnString (connectionString) |> getMongoDatabase "StockCheck"
    let periodMap = MapFromModel.pMap connectionString
    
    member this.Save(si : StockCheck.Model.SalesItem) = 
        let collection = db |> getMongoCollection<SalesItem> "SalesItem"
        MapFromModel.siMap si
        |> collection.Save
        |> ignore
    
    member this.Save(p : StockCheck.Model.Period) = 
        let collection = db |> getMongoCollection<Period> "Period"
        periodMap p
        |> collection.Save
        |> ignore
    
    member this.Save(i : StockCheck.Model.Invoice) = 
        let collection = db |> getMongoCollection<Invoice> "Invoice"
        let dbObject = MapFromModel.iMap i
        dbObject
        |> collection.Save
        |> ignore
    
    member this.Save(s : StockCheck.Model.Supplier) = 
        let collection = db |> getMongoCollection<Invoice> "Supplier"
        
        let supplier = 
            { Supplier._id = MapFromModel.idMap s.Id
              Name = s.Name }
        supplier
        |> collection.Save
        |> ignore
