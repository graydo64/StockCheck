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

    let mapViewToModel (m : StockCheck.Model.SalesItem) (v : SalesItemView) =
        let sut = salesUnitType.fromString v.SalesUnitType
        m.ContainerSize <- v.ContainerSize
        m.CostPerContainer <- v.CostPerContainer
        m.LedgerCode <- v.LedgerCode
        m.Name <- v.Name
        m.SalesPrice <- v.SalesPrice
        m.SalesUnitType <- sut
        m.TaxRate <- v.TaxRate
        m.UllagePerContainer <- v.UllagePerContainer
        if sut = salesUnitType.Other then m.OtherSalesUnit <- v.SalesUnitsPerContainerUnit
        else m.OtherSalesUnit <- 0.
        m

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

    member x.Post(salesItem : SalesItemView) =
        let i = StockCheck.Model.SalesItem()
        mapViewToModel i salesItem
        |> persister.Save
        cache.Remove("#salesItems")

    member x.Put(salesItem : SalesItemView) =
        let i = repo.GetModelSalesItemById(salesItem.Id)
        mapViewToModel i salesItem
        |> persister.Save
        cache.Remove("#salesItems")