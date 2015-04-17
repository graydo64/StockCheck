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
    let mapToInvoiceLineViewModel (invoiceLine : StockCheck.Model.myInvoiceLine) =
        { 
            InvoiceLineViewModel.Id = invoiceLine.Id
            SalesItemId = invoiceLine.SalesItem.Id
            SalesItemDescription =
                String.concat " " [ invoiceLine.SalesItem.ItemName.LedgerCode
                                    invoiceLine.SalesItem.ItemName.Name
                                    "(" + invoiceLine.SalesItem.ItemName.ContainerSize.ToString() + ")" ]
            Quantity = invoiceLine.Quantity
            InvoicedAmountEx = invoiceLine.InvoicedAmountEx / 1.0M<StockCheck.Model.money>
            InvoicedAmountInc = invoiceLine.InvoicedAmountInc / 1.0M<StockCheck.Model.money>
          }

    let mapToInvoiceViewModel (invoice : StockCheck.Model.myInvoice) =
        { InvoiceViewModel.Id = invoice.Id
          Supplier = invoice.Supplier
          InvoiceNumber = invoice.InvoiceNumber
          InvoiceDate = invoice.InvoiceDate
          DeliveryDate = invoice.DeliveryDate
          InvoiceLines =
              List<InvoiceLineViewModel>(invoice.InvoiceLines |> Seq.map (fun il -> mapToInvoiceLineViewModel il))
          TotalEx = invoice.InvoiceLines |> Seq.sumBy (fun il -> il.InvoicedAmountEx / 1.0M<StockCheck.Model.money>)
          TotalInc = invoice.InvoiceLines |> Seq.sumBy (fun il -> il.InvoicedAmountInc / 1.0M<StockCheck.Model.money>) }

type InvoicesPaged =
    { TotalCount : int
      TotalPages : int
      Invoices : InvoiceViewModel seq }

type InvoiceController() =
    inherit ApiController()
    let cache = FsWeb.CacheWrapper()
    let repo = new StockCheck.Repository.Query(FsWeb.Global.Store)

    let mapToInvoiceLine (lv : InvoiceLineViewModel) =

        let salesItem = repo.GetmyModelSalesItemById(lv.SalesItemId)

        {
            StockCheck.Model.myInvoiceLine.Id = lv.Id;
            StockCheck.Model.myInvoiceLine.Quantity = lv.Quantity;
            StockCheck.Model.myInvoiceLine.SalesItem = salesItem;
            StockCheck.Model.myInvoiceLine.InvoicedAmountEx = StockCheck.Model.Conv.money lv.InvoicedAmountEx;
            StockCheck.Model.myInvoiceLine.InvoicedAmountInc = StockCheck.Model.Conv.money lv.InvoicedAmountInc;
        }

    let mapViewToModel (i : InvoiceViewModel) =
        let modelLines = i.InvoiceLines |> Seq.map (fun il -> mapToInvoiceLine il)
        {
            StockCheck.Model.myInvoice.Id = i.Id;
            StockCheck.Model.myInvoice.Supplier = i.Supplier;
            StockCheck.Model.myInvoice.InvoiceNumber = i.InvoiceNumber;
            StockCheck.Model.myInvoice.InvoiceDate = i.InvoiceDate;
            StockCheck.Model.myInvoice.DeliveryDate = i.DeliveryDate;
            StockCheck.Model.myInvoice.InvoiceLines = modelLines;
        }

    let saveInvoice iv =
        let persister = new StockCheck.Repository.Persister(FsWeb.Global.Store)
        mapViewToModel iv |> persister.Save
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
        | "0" -> 
                {
                    Id = String.Empty;
                    Supplier = String.Empty;
                    InvoiceNumber = String.Empty;
                    InvoiceDate = DateTime.MinValue;
                    DeliveryDate = DateTime.MinValue;
                    InvoiceLines = new List<InvoiceLineViewModel>();
                    TotalEx = 0M;
                    TotalInc = 0M;
                }
        | _ -> repo.GetModelInvoice id |> InvoiceControllerHelper.mapToInvoiceViewModel

    member x.Post(invoice : InvoiceViewModel) =
        match (repo.InvoiceExists invoice.InvoiceNumber invoice.Supplier) with
        | true -> x.Request.CreateResponse(System.Net.HttpStatusCode.Conflict)
        | false ->
            saveInvoice invoice
            x.Request.CreateResponse(System.Net.HttpStatusCode.OK)

    member x.Put(invoice : InvoiceViewModel) = saveInvoice invoice