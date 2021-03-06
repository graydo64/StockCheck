﻿namespace FsWeb.Controllers

open System
open System.Net
open System.Net.Http
open System.Web.Http
open StockCheck.Repository
open StockCheck.Model
open StockCheck.Model.Conv
open FsWeb.Model

type SalesItemController() =
    inherit ApiController()
    let cache = new FsWeb.CacheWrapper ()
    let repo = new StockCheck.Repository.Query(FsWeb.Global.Store)
    let persister = new StockCheck.Repository.Persister(FsWeb.Global.Store)
    let cid = "#salesItems"

    let mapViewToM (v : SalesItemView) =
        let baseSI =
            {
                Id = v.Id;
                ItemName = { LedgerCode = v.LedgerCode; Name = v.Name; ContainerSize = v.ContainerSize };
                CostPerContainer = money v.CostPerContainer;
                SalesPrice = money v.SalesPrice;
                TaxRate = percentage v.TaxRate;
                SalesUnitType = salesUnitType.fromString v.SalesUnitType;
                UllagePerContainer = v.UllagePerContainer * 1<pt>;
                OtherSalesUnit = 0.
                ProductCode = v.ProductCode
                IsActive = v.IsActive
            }
        match baseSI.SalesUnitType with
        | Other -> { baseSI with OtherSalesUnit = v.SalesUnitsPerContainerUnit }
        | _ -> baseSI

    member x.Get() =
        match cache.Get cid with
        | Some(i) -> 
            x.Request.CreateResponse(HttpStatusCode.OK, i :?> seq<SalesItemsViewModel>);
        | None -> 
            let i = repo.GetModelSalesItems 
                    |> Seq.map (fun i -> {
                                            SalesItemsViewModel.Id = i.Id;
                                            LedgerCode = i.ItemName.LedgerCode; 
                                            Name = i.ItemName.Name; 
                                            ContainerSize = i.ItemName.ContainerSize; 
                                            CostPerContainer = i.CostPerContainer / 1M<money>; 
                                            SalesPrice = i.SalesPrice / 1M<money>;
                                            SalesUnitType = i.SalesUnitType.toString()
                                            ProductCode = i.ProductCode
                                        })
            cache.Add cid i
            x.Request.CreateResponse(HttpStatusCode.OK, i)

    member x.Get((id : string), ()) =
        let i = repo.GetModelSalesItemById id
        let sii = StockCheck.Model.Factory.getSalesItemInfo i
        { Id = i.Id
          LedgerCode = i.ItemName.LedgerCode
          Name = i.ItemName.Name
          ContainerSize = i.ItemName.ContainerSize
          CostPerContainer = i.CostPerContainer / 1.M<money>
          SalesPrice = i.SalesPrice / 1.M<money>
          TaxRate = i.TaxRate / 1.<percentage>
          UllagePerContainer = i.UllagePerContainer / 1<pt>
          SalesUnitType = i.SalesUnitType.toString()
          SalesUnitsPerContainerUnit = sii.SalesUnitsPerContainerUnit
          CostPerUnitOfSale = sii.CostPerUnitOfSale / 1.M<money>
          MarkUp = sii.MarkUp / 1.M<money>
          IdealGP = sii.IdealGP / 1.<percentage>
          ProductCode = i.ProductCode
          IsActive = i.IsActive
        }

    member x.Post(salesItem : SalesItemView) =
        let pc = match salesItem with
                    | { ProductCode = "" } -> repo.GetNextProductCode()
                    | { ProductCode = null } -> repo.GetNextProductCode()
                    | _ ->  if repo.IsProductCodeInUse salesItem.ProductCode then
                                repo.GetNextProductCode()
                            else
                                salesItem.ProductCode
        mapViewToM { salesItem with ProductCode = pc }
        |> persister.Save
        cache.Remove("#salesItems")

    member x.Put(salesItem : SalesItemView) =
        mapViewToM salesItem
        |> persister.Save
        cache.Remove("#salesItems")