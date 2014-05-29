namespace FsWeb.Controllers

open System.Web
open System.Web.Mvc
open System.Net.Http
open System.Web.Http
open StockCheck.Repository
open System
open System.Linq
open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open System.Runtime.Serialization
open Raven.Client

[<CLIMutable>]
[<DataContract>]
type PeriodItemViewModel = 
    { [<DataMember>] OpeningStock : float
      [<DataMember>] ClosingStock : float
      [<DataMember>] SalesItemId : string
      [<DataMember>] SalesItemLedgerCode : string
      [<DataMember>] SalesItemName : string
      [<DataMember>] Container : float
      [<DataMember>] ItemsReceived : float
      [<DataMember>] SalesQty : float }

[<CLIMutable>]
[<DataContract>]
type PeriodsViewModel = 
    { [<DataMember>] Id : string
      [<DataMember>] Name : string
      [<DataMember>] StartOfPeriod : DateTime
      [<DataMember>] EndOfPeriod : DateTime
      [<DataMember>] SalesEx : decimal
      [<DataMember>] ClosingValueCostEx : decimal }

[<CLIMutable>]
[<DataContract>]
type PeriodViewModel = 
    { [<DataMember>] Id : string
      [<DataMember>] Name : string
      [<DataMember>] StartOfPeriod : DateTime
      [<DataMember>] EndOfPeriod : DateTime
      [<DataMember>] Items : seq<PeriodItemViewModel>
      [<DataMember>] ClosingValueCostEx : decimal
      [<DataMember>] ClosingValueSalesInc : decimal
      [<DataMember>] ClosingValueSalesEx : decimal }

type PeriodsController() = 
    inherit ApiController()
    let repo = new StockCheck.Repository.Query(FsWeb.Global.Store)

    member x.Get() = 
        repo.GetModelPeriods |> Seq.map (fun i -> 
                                    { PeriodsViewModel.Id = i.Id
                                      Name = i.Name
                                      StartOfPeriod = i.StartOfPeriod
                                      EndOfPeriod = i.EndOfPeriod
                                      SalesEx = i.SalesEx
                                      ClosingValueCostEx = i.ClosingValueCostEx })

type PeriodController() = 
    inherit ApiController()
    let repo = new StockCheck.Repository.Query(FsWeb.Global.Store)
    
    let mapToPI (pi : StockCheck.Model.PeriodItem) = 
        { PeriodItemViewModel.OpeningStock = pi.OpeningStock
          PeriodItemViewModel.ClosingStock = pi.ClosingStock
          PeriodItemViewModel.SalesItemId = pi.SalesItem.Id
          PeriodItemViewModel.SalesItemName = pi.SalesItem.Name
          PeriodItemViewModel.SalesItemLedgerCode = pi.SalesItem.LedgerCode
          PeriodItemViewModel.ItemsReceived = pi.ContainersReceived
          SalesQty = pi.Sales
          Container = pi.SalesItem.ContainerSize }
    
    let mapToViewModel (p : StockCheck.Model.Period) = 
        { PeriodViewModel.Id = p.Id
          Name = p.Name
          StartOfPeriod = p.StartOfPeriod
          EndOfPeriod = p.EndOfPeriod
          Items = p.Items |> Seq.map mapToPI
          ClosingValueCostEx = p.ClosingValueCostEx
          ClosingValueSalesInc = decimal p.ClosingValueSalesInc
          ClosingValueSalesEx = decimal p.ClosingValueSalesEx }
    
    let mapPIFromViewModel (pi : PeriodItemViewModel) = 
        let salesItems = repo.GetModelSalesItems
        
        let periodItem = 
            new StockCheck.Model.PeriodItem(salesItems
                                            |> Seq.filter (fun i -> i.Id = pi.SalesItemId)
                                            |> Seq.head)
        periodItem.ClosingStock <- pi.ClosingStock
        periodItem.OpeningStock <- pi.OpeningStock
        periodItem
    
    [<Route("api/period/{id}")>]
    member x.Get(id : string, ()) = mapToViewModel (repo.GetModelPeriodById id)
    
    [<Route("api/period")>]
    member x.Put(period : PeriodViewModel) = 
        let persister = new StockCheck.Repository.Persister(FsWeb.Global.Store)
        let periods = repo.GetModelPeriods |> Seq.filter (fun i -> i.Id = period.Id)
        
        let p = 
            match periods |> Seq.length with
            | 0 -> new StockCheck.Model.Period()
            | _ -> periods |> Seq.head
        p.EndOfPeriod <- period.EndOfPeriod
        p.Name <- period.Name
        p.StartOfPeriod <- period.StartOfPeriod
        p.Items.Clear()
        period.Items
        |> Seq.map (fun i -> mapPIFromViewModel i)
        |> p.Items.AddRange
        persister.Save(p)
    
    [<HttpGet>][<Route("api/period/init-from/{id}")>]
    member x.InitFrom(id : string) = 
        let period = 
            repo.GetModelPeriods
            |> Seq.filter (fun i -> i.Id = id)
            |> Seq.head
        
        let newp = StockCheck.Model.Period.InitialiseFromClone(period)
        mapToViewModel newp
