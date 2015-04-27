namespace FsWeb.Controllers

open System.Web
open System.Net
open System.Net.Http
open System.Web.Http
open StockCheck.Repository
open StockCheck.Model.Factory
open System
open System.Linq
open System.Collections.Generic
open FsWeb.Model
open FsWeb.Model.Mapping.Period

type PeriodController() = 
    inherit ApiController()
    let repo = new StockCheck.Repository.Query(FsWeb.Global.Store)
    let cache = FsWeb.CacheWrapper()
        
    [<Route("api/period/")>]
    member x.Get() = 
        repo.GetModelPeriods |> Seq.map (fun i -> 
                                    let info = StockCheck.Model.Factory.getPeriodInfo i
                                    { PeriodsViewModel.Id = i.Id
                                      Name = i.Name
                                      StartOfPeriod = i.StartOfPeriod
                                      EndOfPeriod = i.EndOfPeriod
                                      SalesEx = info.SalesEx / 1M<StockCheck.Model.money>
                                      ClosingValueCostEx = info.ClosingValueCostEx / 1M<StockCheck.Model.money>
                                    })

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
            newPFromViewModel repo.GetModelSalesItemById period
            |> persister.Save
            x.Request.CreateResponse(HttpStatusCode.OK, period)
        | _ -> 
            x.Request.CreateResponse(HttpStatusCode.Conflict, period)

    [<Route("api/period")>]
    member x.Put(period : PeriodViewModel) = 
        let persister = new StockCheck.Repository.Persister(FsWeb.Global.Store)
        let modelPeriod = repo.GetModelPeriodById period.Id
        let cachedSISeq = Seq.cache repo.GetModelSalesItems
        let getModelSalesItemById id = cachedSISeq |> Seq.where (fun i -> i.Id = id) |> Seq.head
        
        modelPeriod
        |> (fun p -> mapPFromViewModel getModelSalesItemById p period)
        |> persister.Save
        cache.Remove period.Id
        x.Request.CreateResponse(HttpStatusCode.OK, period)
    
    [<HttpGet>][<Route("api/period/init-from/{id}")>]
    member x.InitFrom(id : string) = 
        let period = 
            repo.GetModelPeriods
            |> Seq.filter (fun i -> i.Id = id)
            |> Seq.head
        
        let newp = StockCheck.Model.Factory.initialisePeriodFromClone period
        mapToViewModel newp

    [<HttpGet>][<Route("api/period/init-clean/{id}")>]
    member x.InitFromClean(id : string) = 
        let period = 
            repo.GetModelPeriods
            |> Seq.filter (fun i -> i.Id = id)
            |> Seq.head
        
        let newp = StockCheck.Model.Factory.initialiseWithoutZeroCarriedItems period
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
