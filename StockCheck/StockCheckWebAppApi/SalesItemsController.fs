namespace FsWeb.Controllers

open System.Web
open System.Web.Mvc
open System.Net.Http
open System.Web.Http
open StockCheck.Repository
open System
open System.ComponentModel.DataAnnotations
open System.Runtime.Serialization

[<CLIMutable>]
[<DataContract>]
type SalesItemsViewModel = {
    [<DataMember>]Id : string
    [<DataMember>]LedgerCode : string
    [<DataMember>]Name : string
    [<DataMember>]ContainerSize : float
    [<DataMember>]CostPerContainer : decimal
    [<DataMember>]SalesPrice : decimal
}

type SalesItemsController() =
    inherit ApiController()

    let repo = new StockCheck.Repository.Query("mongodb://localhost")

    member x.Get() =
        repo.GetModelSalesItems |> Seq.map (fun i -> {
                                                        Id = i.Id;
                                                        LedgerCode = i.LedgerCode; 
                                                        Name = i.Name; 
                                                        ContainerSize = i.ContainerSize; 
                                                        CostPerContainer = i.CostPerContainer; 
                                                        SalesPrice = i.SalesPrice
                                                    })
