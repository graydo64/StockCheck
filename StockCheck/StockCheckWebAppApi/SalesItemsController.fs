namespace FsWeb.Controllers

open System.Web.Http
open StockCheck.Repository
open System
open FsWeb.Model

type SalesItemsController() =
    inherit ApiController()

    let cache = FsWeb.CacheWrapper ()
    let repo = new StockCheck.Repository.Query(FsWeb.Global.Store)
    let cid = "#salesItems"

    member x.Get() =
        match cache.Get cid with
        | Some(x) -> x :?> seq<SalesItemsViewModel>
        | None -> 
            let i = repo.GetModelSalesItems 
                    |> Seq.map (fun i -> {
                                            SalesItemsViewModel.Id = i.Id;
                                            LedgerCode = i.LedgerCode; 
                                            Name = i.Name; 
                                            ContainerSize = i.ContainerSize; 
                                            CostPerContainer = i.CostPerContainer; 
                                            SalesPrice = i.SalesPrice
                                        })
            cache.Add cid i
            i
