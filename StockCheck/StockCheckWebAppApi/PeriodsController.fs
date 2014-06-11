namespace FsWeb.Controllers

open System.Web
open System.Net
open System.Net.Http
open System.Web.Http
open StockCheck.Repository
open System
open System.Linq
open System.Collections.Generic
open FsWeb.Model

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
    let salesItems = repo.GetModelSalesItems
    let cache = FsWeb.CacheWrapper()
    
    let mapToPI (pi : StockCheck.Model.PeriodItem) = 
        { PeriodItemViewModel.OpeningStock = pi.OpeningStock
          ClosingStockExpr = pi.ClosingStockExpr
          ClosingStock = pi.ClosingStock
          SalesItemId = pi.SalesItem.Id
          SalesItemName = pi.SalesItem.Name
          SalesItemLedgerCode = pi.SalesItem.LedgerCode
          ItemsReceived = pi.ContainersReceived
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
        let periodItem = 
            new StockCheck.Model.PeriodItem(salesItems
                                            |> Seq.filter (fun i -> i.Id = pi.SalesItemId)
                                            |> Seq.head)
        periodItem.ClosingStock <- pi.ClosingStock
        periodItem.OpeningStock <- pi.OpeningStock
        periodItem.ClosingStockExpr <- pi.ClosingStockExpr
        periodItem
    
    [<Route("api/period/{id}")>]
    member x.Get(id : string, ()) = 
        match cache.Get id with
        | Some(p) -> p :?> PeriodViewModel
        | None -> 
            let vm = mapToViewModel (repo.GetModelPeriodById id)
            cache.Add id vm
            vm
    
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
        persister.Save p
        cache.Remove period.Id
    
    [<HttpGet>][<Route("api/period/init-from/{id}")>]
    member x.InitFrom(id : string) = 
        let period = 
            repo.GetModelPeriods
            |> Seq.filter (fun i -> i.Id = id)
            |> Seq.head
        
        let newp = StockCheck.Model.Period.InitialiseFromClone(period)
        mapToViewModel newp

    [<HttpGet>][<Route("api/period/export/{id}")>]
    member x.Export(id : string) = 
        let p = 
            repo.GetModelPeriodById id

        let i = repo.GetModelInvoicesByDateRange p.StartOfPeriod p.EndOfPeriod

        let f = Excel.Export p i

        let response = new HttpResponseMessage(HttpStatusCode.OK)
        response.Content <- new StreamContent(new System.IO.FileStream(f.FullName, IO.FileMode.Open))
        response.Content.Headers.Add("Content-Disposition", "attachment; filename=" + HttpUtility.UrlEncode(String.Concat(p.Name, f.Extension), Text.Encoding.UTF8))
        response.Content.Headers.Add("Content-Type", "application/vnd.ms-excel")
        response.Content.Headers.Add("Content-Encoding", "UTF-8")
        response.Content.Headers.Expires <- new Nullable<DateTimeOffset>(new DateTimeOffset(System.DateTime.Now))
        response
