namespace FsWeb.Controllers

open System.Linq
open System.Web
open System.Web.Http
open System.Net.Http
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

type InvoicesPaged =
    { TotalCount : int
      TotalPages : int
      Invoices : InvoiceViewModel seq }

type InvoiceController() =
    inherit ApiController()
    let cache = FsWeb.CacheWrapper()
    let repo = new StockCheck.Repository.Query(FsWeb.Global.Store)

    let mapToInvoiceLine (lv : InvoiceLineViewModel) =
        let salesItem = repo.GetModelSalesItemById(lv.SalesItemId)
        let lm = StockCheck.Model.InvoiceLine(salesItem)
        lm.Id <- lv.Id
        lm.Quantity <- lv.Quantity
        lm.InvoicedAmountEx <- lv.InvoicedAmountEx
        lm.InvoicedAmountInc <- lv.InvoicedAmountInc
        lm

    let mapViewToModel (m : StockCheck.Model.Invoice) (i : InvoiceViewModel) =
        let modelLines = i.InvoiceLines |> Seq.map (fun il -> mapToInvoiceLine il)
        m.Id <- i.Id
        m.Supplier <- i.Supplier
        m.InvoiceNumber <- i.InvoiceNumber
        m.InvoiceDate <- i.InvoiceDate
        m.DeliveryDate <- i.DeliveryDate
        m.InvoiceLines <- List<StockCheck.Model.InvoiceLine>(modelLines)
        m

    let saveInvoice iv =
        let persister = new StockCheck.Repository.Persister(FsWeb.Global.Store)
        let im = new StockCheck.Model.Invoice()
        mapViewToModel im iv |> persister.Save
        repo.GetModelPeriods
        |> Seq.where (fun p -> p.StartOfPeriod <= iv.DeliveryDate && p.EndOfPeriod >= iv.DeliveryDate)
        |> Seq.iter (fun p -> cache.Remove p.Id)

    member x.Get() = repo.GetModelInvoices() |> Seq.map InvoiceControllerHelper.mapToInvoiceViewModel

    [<Route("api/Invoice/{pageSize:int}/{pageNumber:int}")>]
    member x.Get((pageSize : int), (pageNumber : int)) =
        let totalCount = repo.GetInvoiceCount
        let invoices =
            repo.GetModelInvoicesPaged pageSize pageNumber |> Seq.map InvoiceControllerHelper.mapToInvoiceViewModel
        { TotalCount = totalCount;
            TotalPages = int (Math.Ceiling(float totalCount/float pageSize))
          Invoices = invoices }

    member x.Get(id) =
        match id with
        | "0" -> StockCheck.Model.Invoice() |> InvoiceControllerHelper.mapToInvoiceViewModel
        | _ -> repo.GetModelInvoice id |> InvoiceControllerHelper.mapToInvoiceViewModel

    member x.Post(invoice : InvoiceViewModel) =
        match (repo.InvoiceExists invoice.InvoiceNumber invoice.Supplier) with
        | true -> x.Request.CreateResponse(System.Net.HttpStatusCode.Conflict)
        | false ->
            saveInvoice invoice
            x.Request.CreateResponse(System.Net.HttpStatusCode.OK)

    member x.Put(invoice : InvoiceViewModel) = saveInvoice invoice