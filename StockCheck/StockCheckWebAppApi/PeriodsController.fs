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
type PeriodsViewModel = {
    [<DataMember>]Name : string
    [<DataMember>]StartOfPeriod : DateTime
    [<DataMember>]EndOfPeriod : DateTime
    [<DataMember>]ClosingValueCostEx : decimal
}

type PeriodsController() =
    inherit ApiController()

    let repo = new StockCheck.Repository.Query("mongodb://localhost")

    member x.Get() =
        repo.GetModelPeriods |> Seq.map (fun i -> {Name = i.Name; StartOfPeriod = i.StartOfPeriod; EndOfPeriod = i.EndOfPeriod; ClosingValueCostEx = i.ClosingValueCostEx})
