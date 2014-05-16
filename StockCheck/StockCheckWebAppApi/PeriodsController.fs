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
type PeriodItemViewModel = {
    [<DataMember>]OpeningStock : float
    [<DataMember>]ClosingStock : float
    [<DataMember>]SalesItemId : string
    [<DataMember>]SalesItemLedgerCode : string
    [<DataMember>]SalesItemName : string
    [<DataMember>]ItemsReceived : float
}

[<CLIMutable>]
[<DataContract>]
type PeriodsViewModel = {
    [<DataMember>]Id : string
    [<DataMember>]Name : string
    [<DataMember>]StartOfPeriod : DateTime
    [<DataMember>]EndOfPeriod : DateTime
    [<DataMember>]ClosingValueCostEx : decimal
}

[<CLIMutable>]
[<DataContract>]
type PeriodViewModel = {
    [<DataMember>]Id : string
    [<DataMember>]Name : string
    [<DataMember>]StartOfPeriod : DateTime
    [<DataMember>]EndOfPeriod : DateTime
    [<DataMember>]Items : seq<PeriodItemViewModel>
    [<DataMember>]ClosingValueCostEx : decimal
    [<DataMember>]ClosingValueSalesInc : decimal
    [<DataMember>]ClosingValueSalesEx : decimal
}

type PeriodsController() =
    inherit ApiController()

    let repo = new StockCheck.Repository.Query("mongodb://localhost")

    member x.Get() =
        repo.GetModelPeriods |> Seq.map (fun i -> {PeriodsViewModel.Id = i.Id; Name = i.Name; StartOfPeriod = i.StartOfPeriod; EndOfPeriod = i.EndOfPeriod; ClosingValueCostEx = i.ClosingValueCostEx})

type PeriodController() =
    inherit ApiController()

    let repo = new StockCheck.Repository.Query("mongodb://localhost")

    let mapToPI (pi: StockCheck.Model.PeriodItem) =
        {
            PeriodItemViewModel.OpeningStock = pi.OpeningStock;
            PeriodItemViewModel.ClosingStock = pi.ClosingStock;
            PeriodItemViewModel.SalesItemId = pi.SalesItem.Id;
            PeriodItemViewModel.SalesItemName = pi.SalesItem.Name;
            PeriodItemViewModel.SalesItemLedgerCode = pi.SalesItem.LedgerCode;
            PeriodItemViewModel.ItemsReceived = pi.ItemsReceived |> Seq.sumBy (fun i -> i.Quantity)
        }

    let mapToViewModel (p: StockCheck.Model.Period) =
        {
            PeriodViewModel.Id = p.Id;
            Name = p.Name;
            StartOfPeriod = p.StartOfPeriod;
            EndOfPeriod = p.EndOfPeriod;
            Items = p.Items |> Seq.map mapToPI;
            ClosingValueCostEx = p.ClosingValueCostEx;
            ClosingValueSalesInc = decimal p.ClosingValueSalesInc;
            ClosingValueSalesEx = decimal p.ClosingValueSalesEx;
        }
           
    [<Route("api/period/{id}")>]    
    member x.Get(id : string, ()) =
        let p = repo.GetModelPeriods |> Seq.filter (fun i -> i.Id = id) |> Seq.head
        mapToViewModel p

    [<Route("api/period")>]    
    member x.Put(period : PeriodViewModel) =
        let persister = new StockCheck.Repository.Persister("mongodb://localhost")

        let periods = repo.GetModelPeriods |> Seq.filter (fun i -> i.Id = period.Id)
        let p = match periods |> Seq.length with
                | 0 -> new StockCheck.Model.Period()
                | _ -> periods |> Seq.head
        p.EndOfPeriod <- period.EndOfPeriod
        p.Name <- period.Name
        p.StartOfPeriod <- period.StartOfPeriod
        persister.Save(p)

    [<HttpGet>]
    [<Route("api/period/init-from/{id}")>]
    member x.InitFrom(id : string) =
        let period = repo.GetModelPeriods |> Seq.filter (fun i -> i.Id = id) |> Seq.head
        let newp = StockCheck.Model.Period.InitialiseFromClone(period)
        mapToViewModel newp
