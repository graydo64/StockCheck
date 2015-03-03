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

type PeriodController() = 
    inherit ApiController()
    let repo = new StockCheck.Repository.Query(FsWeb.Global.Store)
    let salesItems = repo.GetModelSalesItems
    let cache = FsWeb.CacheWrapper()
    
    let mapToPI (pi : StockCheck.Model.PeriodItem) = 
        { PeriodItemViewModel.Id = pi.Id
          OpeningStock = pi.OpeningStock
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
    
    let mapPIFromViewModel (p : StockCheck.Model.Period) (pi : PeriodItemViewModel) =  
        let periodItem = match p.Items.Where(fun a -> a.SalesItem.Id = pi.SalesItemId).Any() with
                            | true -> p.Items.Where(fun a -> a.SalesItem.Id = pi.SalesItemId).First()
                            | false -> let ni = new StockCheck.Model.PeriodItem(
                                                                        salesItems
                                                                        |> Seq.filter(fun i -> i.Id = pi.SalesItemId)
                                                                        |> Seq.head) 
                                       p.Items.Add(ni)
                                       ni
                                            
        periodItem.ClosingStock <- pi.ClosingStock
        periodItem.OpeningStock <- pi.OpeningStock
        periodItem.ClosingStockExpr <- pi.ClosingStockExpr

    let mapPFromViewModel (p : StockCheck.Model.Period) (vm : PeriodViewModel) =
        p.EndOfPeriod <- vm.EndOfPeriod
        p.Name <- vm.Name
        p.StartOfPeriod <- vm.StartOfPeriod
        vm.Items
        |> Seq.iter (fun i -> mapPIFromViewModel p i)
        p

    [<Route("api/period/")>]
    member x.Get() = 
        repo.GetModelPeriods |> Seq.map (fun i -> 
                                    { PeriodsViewModel.Id = i.Id
                                      Name = i.Name
                                      StartOfPeriod = i.StartOfPeriod
                                      EndOfPeriod = i.EndOfPeriod
                                      SalesEx = i.SalesEx
                                      ClosingValueCostEx = i.ClosingValueCostEx })

    [<Route("api/period/{id}")>]
    member x.Get(id : string, ()) = 
        match cache.Get id with
        | Some(p) -> p :?> PeriodViewModel
        | None -> 
            let vm = mapToViewModel (repo.GetModelPeriodById id)
            cache.Add id vm
            vm
    
    [<Route("api/period")>]
    member x.Post(period : PeriodViewModel) =
        let persister = new StockCheck.Repository.Persister(FsWeb.Global.Store)
        let periods = repo.GetModelPeriods |> Seq.filter (fun i -> i.Id = period.Id) |> Seq.length
        
        match periods with
        | 0 -> 
            let p = new StockCheck.Model.Period()
            mapPFromViewModel p period
            |> persister.Save
            x.Request.CreateResponse(HttpStatusCode.OK, period)
        | _ -> 
            x.Request.CreateResponse(HttpStatusCode.Conflict, period)

    [<Route("api/period")>]
    member x.Put(period : PeriodViewModel) = 
        let persister = new StockCheck.Repository.Persister(FsWeb.Global.Store)
        let periods = repo.GetModelPeriods |> Seq.filter (fun i -> i.Id = period.Id)
        
        match periods |> Seq.length with
        | 0 -> 
            x.Request.CreateResponse(HttpStatusCode.NotFound, period)
        | _ -> 
            periods 
            |> Seq.head
            |> (fun p -> mapPFromViewModel p period)
            |> persister.Save
            cache.Remove period.Id
            x.Request.CreateResponse(HttpStatusCode.OK, period)
    
    [<HttpGet>][<Route("api/period/init-from/{id}")>]
    member x.InitFrom(id : string) = 
        let period = 
            repo.GetModelPeriods
            |> Seq.filter (fun i -> i.Id = id)
            |> Seq.head
        
        let newp = StockCheck.Model.Period.InitialiseFromClone(period)
        mapToViewModel newp

    [<HttpGet>][<Route("api/period/init-clean/{id}")>]
    member x.InitFromClean(id : string) = 
        let period = 
            repo.GetModelPeriods
            |> Seq.filter (fun i -> i.Id = id)
            |> Seq.head
        
        let newp = StockCheck.Model.Period.InitialiseWithoutZeroCarriedItems(period)
        mapToViewModel newp

    [<HttpGet>][<Route("api/period/export/{id}")>]
    member x.Export(id : string) = 
        let p = 
            repo.GetModelPeriodById id

        let i = repo.GetModelInvoicesByDateRange p.StartOfPeriod p.EndOfPeriod

        let f = Excel.Export p i

        let response = x.Request.CreateResponse(HttpStatusCode.OK)
        response.Content <- new StreamContent(new System.IO.FileStream(f.FullName, IO.FileMode.Open))
        response.Content.Headers.Add("Content-Disposition", "attachment; filename=" + HttpUtility.UrlEncode(String.Concat(p.Name, f.Extension), Text.Encoding.UTF8))
        response.Content.Headers.Add("Content-Type", "application/vnd.ms-excel")
        response.Content.Headers.Add("Content-Encoding", "UTF-8")
        response.Content.Headers.Expires <- new Nullable<DateTimeOffset>(new DateTimeOffset(System.DateTime.Now))
        response
