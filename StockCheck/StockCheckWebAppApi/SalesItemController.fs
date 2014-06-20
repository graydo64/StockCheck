namespace FsWeb.Controllers

open System
open System.Net
open System.Net.Http
open System.Web.Http
open StockCheck.Repository
open StockCheck.Model
open FsWeb.Model

type SalesItemController() =
    inherit ApiController()
    let cache = new FsWeb.CacheWrapper ()
    let repo = new StockCheck.Repository.Query(FsWeb.Global.Store)
    let persister = new StockCheck.Repository.Persister(FsWeb.Global.Store)
    let cid = "#salesItems"

    member x.Get() =
        match cache.Get cid with
        | Some(i) -> 
            x.Request.CreateResponse(HttpStatusCode.OK, i :?> seq<SalesItemsViewModel>);
        | None -> 
            let i = repo.GetModelSalesItems 
                    |> Seq.map (fun i -> {
                                            SalesItemsViewModel.Id = i.Id;
                                            LedgerCode = i.LedgerCode; 
                                            Name = i.Name; 
                                            ContainerSize = i.ContainerSize; 
                                            CostPerContainer = i.CostPerContainer; 
                                            SalesPrice = i.SalesPrice
                                            SalesUnitType = i.SalesUnitType.toString()
                                        })
            cache.Add cid i
            x.Request.CreateResponse(HttpStatusCode.OK, i)

    member x.Get((id : string), ()) =
        let i = repo.GetModelSalesItemById id
        { Id = i.Id
          LedgerCode = i.LedgerCode
          Name = i.Name
          ContainerSize = i.ContainerSize
          CostPerContainer = i.CostPerContainer
          SalesPrice = i.SalesPrice
          TaxRate = i.TaxRate
          UllagePerContainer = i.UllagePerContainer
          SalesUnitType = i.SalesUnitType.toString()
          SalesUnitsPerContainerUnit = i.SalesUnitsPerContainerUnit
          CostPerUnitOfSale = i.CostPerUnitOfSale
          MarkUp = i.MarkUp
          IdealGP = i.IdealGP }

    member x.Put(salesItem : SalesItemView) =
        let sut = salesUnitType.fromString salesItem.SalesUnitType
        let i = StockCheck.Model.SalesItem()
        i.Id <- salesItem.Id
        i.ContainerSize <- salesItem.ContainerSize
        i.CostPerContainer <- salesItem.CostPerContainer
        i.LedgerCode <- salesItem.LedgerCode
        i.Name <- salesItem.Name
        i.SalesPrice <- salesItem.SalesPrice
        i.SalesUnitType <- sut
        i.TaxRate <- salesItem.TaxRate
        i.UllagePerContainer <- salesItem.UllagePerContainer
        if sut = salesUnitType.Other then i.OtherSalesUnit <- salesItem.SalesUnitsPerContainerUnit
        else i.OtherSalesUnit <- 0.
        persister.Save i
        cache.Remove("#salesItems")