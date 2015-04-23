﻿namespace StockCheck.Repository

open System
open System.Collections.Generic
open System.Linq
open System.ComponentModel.DataAnnotations
open Raven.Client
open Raven.Client.Document
open StockCheck.Model.Conv

type SalesItem = 
    { mutable Id : string
      ContainerSize : float
      CostPerContainer : decimal
      LedgerCode : string
      Name : string
      mutable SalesPrice : decimal
      TaxRate : float
      UllagePerContainer : int
      SalesUnitType : string
      OtherSalesUnit : float }

type ItemReceived = 
    { SalesItemId : string
      Quantity : float
      ReceivedDate : DateTime
      InvoicedAmountEx : decimal
      InvoicedAmountInc : decimal }

type PeriodItem = 
    { mutable Id : string
      PeriodId : string
      SalesItem : SalesItem
      OpeningStock : float
      ClosingStockExpr : string
      ClosingStock : float }

type Period = 
    { mutable Id : string
      Name : string
      StartOfPeriod : DateTime
      EndOfPeriod : DateTime
      mutable Items : PeriodItem seq }

type InvoiceLine = 
    { mutable Id : string
      SalesItem : SalesItem
      Quantity : float
      InvoicedAmountEx : decimal
      InvoicedAmountInc : decimal }

type Invoice = 
    { mutable Id : string
      Supplier : string
      InvoiceNumber : string
      InvoiceDate : DateTime
      DeliveryDate : DateTime
      InvoiceLines : InvoiceLine seq }

type Supplier = 
    { mutable Id : string
      Name : string }

module internal MapToModel = 
    let irMap (ir : ItemReceived) = 
        {
            StockCheck.Model.myItemReceived.Id = String.Empty;
            StockCheck.Model.myItemReceived.Quantity = ir.Quantity;
            StockCheck.Model.myItemReceived.ReceivedDate = ir.ReceivedDate;
            StockCheck.Model.myItemReceived.InvoicedAmountEx = money ir.InvoicedAmountEx;
            StockCheck.Model.myItemReceived.InvoicedAmountInc = money ir.InvoicedAmountInc
        }

//    let siMap (si : SalesItem) = 
//        match si with
//        | i when String.IsNullOrEmpty(i.Id) -> StockCheck.Model.SalesItem()
//        | _ -> StockCheck.Model.SalesItem
//                (Id = si.Id.ToString(), ContainerSize = si.ContainerSize, CostPerContainer = si.CostPerContainer, 
//                    LedgerCode = si.LedgerCode, Name = si.Name, SalesPrice = si.SalesPrice, TaxRate = si.TaxRate, 
//                    UllagePerContainer = si.UllagePerContainer, SalesUnitType = StockCheck.Model.salesUnitType.fromString si.SalesUnitType,
//                    OtherSalesUnit = si.OtherSalesUnit)

    let mysiMap (si : SalesItem) = 
        match si with
        | i when String.IsNullOrEmpty(i.Id) ->
            StockCheck.Model.Factory.defaultMySalesItem
        | _ -> 
            {
                StockCheck.Model.mySalesItem.Id = si.Id.ToString();
                StockCheck.Model.mySalesItem.ItemName = { LedgerCode = si.LedgerCode; Name = si.Name; ContainerSize = si.ContainerSize }
                StockCheck.Model.mySalesItem.CostPerContainer = StockCheck.Model.Conv.money si.CostPerContainer;
                StockCheck.Model.mySalesItem.SalesPrice = StockCheck.Model.Conv.money si.SalesPrice;
                StockCheck.Model.mySalesItem.TaxRate = StockCheck.Model.Conv.percentage si.TaxRate;
                StockCheck.Model.mySalesItem.UllagePerContainer = si.UllagePerContainer * 1<StockCheck.Model.pt>;
                StockCheck.Model.mySalesItem.SalesUnitType = StockCheck.Model.salesUnitType.fromString si.SalesUnitType;
                StockCheck.Model.mySalesItem.OtherSalesUnit = si.OtherSalesUnit;
            }
    
    let piMap (pi : PeriodItem) = 
        {
            StockCheck.Model.myPeriodItem.Id = pi.Id;
            StockCheck.Model.myPeriodItem.OpeningStock = pi.OpeningStock;
            StockCheck.Model.myPeriodItem.ClosingStockExpr = pi.ClosingStockExpr;
            StockCheck.Model.myPeriodItem.ClosingStock = pi.ClosingStock;
            StockCheck.Model.myPeriodItem.SalesItem = mysiMap pi.SalesItem;
            StockCheck.Model.myPeriodItem.ItemsReceived = [];
        }
    
    let pMap (p : Period) = 
        let items = p.Items |> Seq.map piMap
        {
            StockCheck.Model.myPeriod.Id = p.Id.ToString(); 
            StockCheck.Model.myPeriod.Name = p.Name; 
            StockCheck.Model.myPeriod.StartOfPeriod = p.StartOfPeriod.ToLocalTime(); 
            StockCheck.Model.myPeriod.EndOfPeriod = p.EndOfPeriod.ToLocalTime();
            StockCheck.Model.myPeriod.Items = items
        }
    
    let ilMap (il : InvoiceLine) = 
        let salesItem = mysiMap il.SalesItem
        let (salesItem : StockCheck.Model.mySalesItem) = {
            Id = il.SalesItem.Id;
            ItemName = { LedgerCode = il.SalesItem.LedgerCode; Name = il.SalesItem.Name; ContainerSize = il.SalesItem.ContainerSize };
            CostPerContainer = money il.SalesItem.CostPerContainer;
            SalesPrice = money il.SalesItem.SalesPrice;
            TaxRate = percentage il.SalesItem.TaxRate;
            SalesUnitType = StockCheck.Model.salesUnitType.fromString il.SalesItem.SalesUnitType;
            OtherSalesUnit = il.SalesItem.OtherSalesUnit;
            UllagePerContainer = il.SalesItem.UllagePerContainer * 1<StockCheck.Model.pt>;
        }
        {
            StockCheck.Model.myInvoiceLine.Id = il.Id.ToString();
            StockCheck.Model.myInvoiceLine.Quantity = il.Quantity;
            StockCheck.Model.myInvoiceLine.SalesItem = salesItem;
            StockCheck.Model.myInvoiceLine.InvoicedAmountEx = money il.InvoicedAmountEx;
            StockCheck.Model.myInvoiceLine.InvoicedAmountInc = money il.InvoicedAmountInc;
        }
    
    let iMap (i : Invoice) = 
        let lines = i.InvoiceLines |> Seq.map ilMap
        {
            StockCheck.Model.myInvoice.Id = i.Id.ToString(); 
            StockCheck.Model.myInvoice.Supplier = i.Supplier; 
            StockCheck.Model.myInvoice.InvoiceNumber = i.InvoiceNumber; 
            StockCheck.Model.myInvoice.InvoiceDate = i.InvoiceDate.ToLocalTime(); 
            StockCheck.Model.myInvoice.DeliveryDate = i.DeliveryDate.ToLocalTime(); 
            StockCheck.Model.myInvoice.InvoiceLines = lines;
        }
    
    let supMap (s : Supplier) = 
        { StockCheck.Model.mySupplier.Id = s.Id.ToString(); StockCheck.Model.mySupplier.Name = s.Name }

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
        session.Query<Invoice>().Take(1024).ToList()

    member internal this.GetInvoicesPaged s p =
        use session = documentStore.OpenSession(dbName)
        session.Query<Invoice>().OrderByDescending(fun i -> i.InvoiceDate).ThenByDescending(fun i -> i.DeliveryDate).Skip((p - 1) * s).Take(s).ToList()

    member this.GetInvoiceCount =
        use session = documentStore.OpenSession(dbName)
        session.Query<Invoice>().Count()
    
    member internal this.GetInvoicesByDateRange startDate endDate = 
        use session = documentStore.OpenSession(dbName)
        session.Query<Invoice>().Where(fun i -> i.DeliveryDate >= startDate && i.DeliveryDate <= endDate).ToList()

    member internal this.TestForAnyInvoicesMatching number supplier =
        use session = documentStore.OpenSession(dbName)
        session.Query<Invoice>().Where(fun i -> i.InvoiceNumber = number && i.Supplier = supplier).Any()
    
    //member this.GetModelSalesItemById id = this.GetSalesItemById id |> MapToModel.siMap

    member this.GetmyModelSalesItemById id = this.GetSalesItemById id |> MapToModel.mysiMap
    
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
                { Id = String.Empty
                  PeriodItem.PeriodId = id
                  SalesItem = salesItem
                  OpeningStock = 0.
                  ClosingStockExpr = String.Empty
                  ClosingStock = 0.}
        
        let invoicePeriodItems = 
                invoices
                |> Seq.collect (fun i -> i.InvoiceLines |> Seq.map (fun il -> il.SalesItem))
                |> Seq.distinct
                |> Seq.map getPeriodItem

        let periodItems = Seq.concat [invoicePeriodItems; p.Items] |> Seq.distinct
        let basePeriod = StockCheck.Model.Factory.getPeriod p.Id p.Name p.StartOfPeriod p.EndOfPeriod
        let modelPeriod = { basePeriod with Items = periodItems |> Seq.map MapToModel.piMap }

        let mapItemsReceived sid =             
            itemsReceived.Where(fun r -> r.SalesItemId = sid)
            |> Seq.map (fun ir ->  {
                                        StockCheck.Model.myItemReceived.Id = String.Empty;
                                        StockCheck.Model.myItemReceived.InvoicedAmountEx = money ir.InvoicedAmountEx;
                                        StockCheck.Model.myItemReceived.InvoicedAmountInc = money ir.InvoicedAmountInc;
                                        StockCheck.Model.myItemReceived.Quantity = ir.Quantity;
                                        StockCheck.Model.myItemReceived.ReceivedDate = ir.ReceivedDate;
                                    })

        let newItemSeq = modelPeriod.Items
                            |> Seq.map ( fun mpi -> 
                                                let ir = mapItemsReceived mpi.SalesItem.Id
                                                { mpi with ItemsReceived = ir })

        { modelPeriod with Items = newItemSeq }
    
    member this.GetModelPeriods = this.GetPeriods() |> Seq.map (fun p -> this.GetModelPeriodById p.Id)
    member this.GetModelSalesItems = this.GetSalesItems() |> Seq.map MapToModel.mysiMap
    member this.GetModelInvoice id = this.GetInvoice id |> MapToModel.iMap
    member this.GetModelInvoices() = this.GetInvoices() |> Seq.map MapToModel.iMap
    member this.GetModelInvoicesPaged (pageSize: int) (pageNumber: int) = this.GetInvoicesPaged pageSize pageNumber |> Seq.map MapToModel.iMap
    member this.GetModelInvoicesByDateRange start finish = this.GetInvoicesByDateRange start finish |> Seq.map MapToModel.iMap
    member this.InvoiceExists n s = this.TestForAnyInvoicesMatching n s
    member this.GetModelSuppliers = this.GetSuppliers() |> Seq.map MapToModel.supMap

module internal MapFromModel = 
    let idMap id = match id with
                    | x when String.IsNullOrEmpty(x) -> Guid.NewGuid().ToString()
                    | _ -> id
        
    let piMap documentStore id (pi : StockCheck.Model.myPeriodItem) = 
        let query = new Query(documentStore)
        { Id = idMap pi.Id
          PeriodId = id
          SalesItem = query.GetSalesItemById pi.SalesItem.Id
          OpeningStock = pi.OpeningStock
          ClosingStockExpr = pi.ClosingStockExpr
          ClosingStock = pi.ClosingStock }

    let mysiMap (si : StockCheck.Model.mySalesItem) = 
        { Id = idMap si.Id
          ContainerSize = si.ItemName.ContainerSize
          CostPerContainer = si.CostPerContainer / 1.0M<StockCheck.Model.money>
          LedgerCode = si.ItemName.LedgerCode
          Name = si.ItemName.Name
          SalesPrice = si.SalesPrice / 1.0M<StockCheck.Model.money>
          TaxRate = si.TaxRate / 1.0<StockCheck.Model.percentage>
          UllagePerContainer = si.UllagePerContainer / 1<StockCheck.Model.pt>
          SalesUnitType = si.SalesUnitType.toString()
          OtherSalesUnit = si.OtherSalesUnit }
    
//    let siMap (si : StockCheck.Model.SalesItem) = 
//        { Id = idMap si.Id
//          ContainerSize = si.ContainerSize
//          CostPerContainer = si.CostPerContainer
//          LedgerCode = si.LedgerCode
//          Name = si.Name
//          SalesPrice = si.SalesPrice
//          TaxRate = si.TaxRate
//          UllagePerContainer = si.UllagePerContainer
//          SalesUnitType = si.SalesUnitType.toString()
//          OtherSalesUnit = si.OtherSalesUnit }
    
    let pMap (documentStore) (p : StockCheck.Model.myPeriod) = 
        { Id = idMap p.Id
          Name = p.Name
          StartOfPeriod = p.StartOfPeriod
          EndOfPeriod = p.EndOfPeriod
          Items = 
              p.Items
              |> Seq.map (fun i -> piMap documentStore p.Id i)
              |> List.ofSeq }

    let periodMap (p : StockCheck.Model.myPeriod) =
        { Id = idMap p.Id
          Name = p.Name
          StartOfPeriod = p.StartOfPeriod
          EndOfPeriod = p.EndOfPeriod
          Items = []}

    
    let ilMap (il : StockCheck.Model.myInvoiceLine) = 
        { Id = idMap il.Id
          SalesItem = mysiMap il.SalesItem
          Quantity = il.Quantity
          InvoicedAmountEx = il.InvoicedAmountEx / 1.0M<StockCheck.Model.money>
          InvoicedAmountInc = il.InvoicedAmountInc / 1.0M<StockCheck.Model.money> }
    
    let iMap (i : StockCheck.Model.myInvoice) = 
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

    member this.Save(si : StockCheck.Model.mySalesItem) = 
        MapFromModel.mysiMap si |> saveDocument
    
    member this.Save(p : StockCheck.Model.myPeriod) = 
        periodMap p |> saveDocument
    
    member this.Save(i : StockCheck.Model.myInvoice) = 
        MapFromModel.iMap i |> saveDocument
    
    member this.Save(s : StockCheck.Model.mySupplier) = 
        let supplier = 
            { Supplier.Id = MapFromModel.idMap s.Id
              Name = s.Name }
        supplier |> saveDocument
