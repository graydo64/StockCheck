namespace FsWeb.Controllers

open System.Web
open System.Web.Mvc
open System.Net.Http
open System.Web.Http
open StockCheck.Repository
open System
open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open System.Runtime.Serialization

[<CLIMutable>]
[<DataContract>]
type ItemReceivedViewModel = {
    [<DataMember>]Quantity : float
    [<DataMember>]ReceivedDate : DateTime
    [<DataMember>]InvoicedAmountEx : decimal
    [<DataMember>]InvoicedAmountInc : decimal
    [<DataMember>]Reference : string
}

[<CLIMutable>]
[<DataContract>]
type InvoiceLineViewModel = {
    [<DataMember>]Id : string
    [<DataMember>]SalesItemId : string
    [<DataMember>]Quantity : float
    [<DataMember>]InvoicedAmountEx : decimal
    [<DataMember>]InvoicedAmountInc : decimal
}

[<CLIMutable>]
[<DataContract>]
type InvoiceViewModel = {
    [<DataMember>]Id : string
    [<DataMember>]Supplier : string
    [<DataMember>]InvoiceNumber : string
    [<DataMember>]InvoiceDate : DateTime
    [<DataMember>]DeliveryDate : DateTime
    [<DataMember>]InvoiceLines : List<InvoiceLineViewModel>
}

type InvoiceController() =
    inherit ApiController()

    let repo = new StockCheck.Repository.Query("mongodb://localhost")

    let mapToInvoiceLineViewModel (invoiceLine : StockCheck.Model.InvoiceLine) =
        {
            InvoiceLineViewModel.Id = invoiceLine.Id
            SalesItemId = invoiceLine.SalesItem.Id
            Quantity = invoiceLine.Quantity
            InvoicedAmountEx = invoiceLine.InvoicedAmountEx
            InvoicedAmountInc = invoiceLine.InvoicedAmountInc
        }

    let mapToInvoiceViewModel (invoice : StockCheck.Model.Invoice) = 
        {
            InvoiceViewModel.Id = invoice.Id
            Supplier = invoice.Supplier
            InvoiceNumber = invoice.InvoiceNumber
            InvoiceDate = invoice.InvoiceDate
            DeliveryDate = invoice.DeliveryDate
            InvoiceLines = List<InvoiceLineViewModel>(invoice.InvoiceLines |> Seq.map(fun il -> mapToInvoiceLineViewModel il))
        }

    member x.Get() =
        {
            Id = "1234"
            Supplier = "Tunstall"
            InvoiceNumber = "X12345"
            InvoiceDate = DateTime.Now.Date
            DeliveryDate = DateTime.Now.Date.AddDays(-1.)
            InvoiceLines = List<InvoiceLineViewModel>([|
            {
                Id = "123456789"
                SalesItemId = ""
                Quantity = 2.
                InvoicedAmountEx = decimal 134.5
                InvoicedAmountInc = decimal 0.
            }|])
        }
