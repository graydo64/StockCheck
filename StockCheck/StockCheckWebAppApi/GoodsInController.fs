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

type GoodsInController() =
    inherit ApiController()

    let repo = new StockCheck.Repository.Query("mongodb://localhost")
