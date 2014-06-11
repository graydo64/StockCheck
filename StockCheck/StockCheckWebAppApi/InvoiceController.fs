namespace FsWeb.Controllers

open System.Web.Http
open StockCheck.Repository
open System
open System.Collections.Generic
open FsWeb.Model

module InvoiceControllerHelper = 
    let mapToInvoiceLineViewModel (invoiceLine : StockCheck.Model.InvoiceLine) = 
        { InvoiceLineViewModel.Id = invoiceLine.Id
          SalesItemId = invoiceLine.SalesItem.Id
          SalesItemDescription = 
              String.concat " " [ invoiceLine.SalesItem.LedgerCode
                                  invoiceLine.SalesItem.Name
                                  "(" + invoiceLine.SalesItem.ContainerSize.ToString() + ")" ]
          Quantity = invoiceLine.Quantity
          InvoicedAmountEx = invoiceLine.InvoicedAmountEx
          InvoicedAmountInc = invoiceLine.InvoicedAmountInc }
    
    let mapToInvoiceViewModel (invoice : StockCheck.Model.Invoice) = 
        { InvoiceViewModel.Id = invoice.Id
          Supplier = invoice.Supplier
          InvoiceNumber = invoice.InvoiceNumber
          InvoiceDate = invoice.InvoiceDate
          DeliveryDate = invoice.DeliveryDate
          InvoiceLines = 
              List<InvoiceLineViewModel>(invoice.InvoiceLines |> Seq.map (fun il -> mapToInvoiceLineViewModel il))
          TotalEx = invoice.InvoiceLines |> Seq.sumBy (fun il -> il.InvoicedAmountEx)
          TotalInc = invoice.InvoiceLines |> Seq.sumBy (fun il -> il.InvoicedAmountInc) }

type InvoicesController() = 
    inherit ApiController()
    let repo = new StockCheck.Repository.Query(FsWeb.Global.Store)
    member x.Get() = repo.GetModelInvoices() |> Seq.map InvoiceControllerHelper.mapToInvoiceViewModel

type InvoiceController() = 
    inherit ApiController()
    let repo = new StockCheck.Repository.Query(FsWeb.Global.Store)
    
    let mapToInvoiceLine (invoiceLine : InvoiceLineViewModel) = 
        let salesItem = repo.GetModelSalesItemById(invoiceLine.SalesItemId)
        let line = StockCheck.Model.InvoiceLine(salesItem)
        line.Id <- invoiceLine.Id
        line.Quantity <- invoiceLine.Quantity
        line.InvoicedAmountEx <- invoiceLine.InvoicedAmountEx
        line.InvoicedAmountInc <- invoiceLine.InvoicedAmountInc
        line
    
    member x.Get(id) = 
        match id with
        | "0" -> StockCheck.Model.Invoice() |> InvoiceControllerHelper.mapToInvoiceViewModel
        | _ -> repo.GetModelInvoice id |> InvoiceControllerHelper.mapToInvoiceViewModel
    
    member x.Put(invoice : InvoiceViewModel) = 
        let persister = new StockCheck.Repository.Persister(FsWeb.Global.Store)
        let modelInvoice = new StockCheck.Model.Invoice()
        let modelLines = invoice.InvoiceLines |> Seq.map (fun il -> mapToInvoiceLine il)
        modelInvoice.Id <- invoice.Id
        modelInvoice.Supplier <- invoice.Supplier
        modelInvoice.InvoiceNumber <- invoice.InvoiceNumber
        modelInvoice.InvoiceDate <- invoice.InvoiceDate
        modelInvoice.DeliveryDate <- invoice.DeliveryDate
        modelInvoice.InvoiceLines <- List<StockCheck.Model.InvoiceLine>(modelLines)
        persister.Save(modelInvoice)
