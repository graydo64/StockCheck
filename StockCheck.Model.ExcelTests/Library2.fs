module StockCheck.Model.TestUtilities

open NUnit.Framework
open FsUnit
open System.IO
open System.Linq
open System.Xml
open StockCheck.Model
open StockCheck.Model.Conv
open StockCheck.Repository
open Raven.Client
open Raven.Client.Document

let store = new DocumentStore()
store.Url <- "http://localhost:8880/"
store.Conventions.IdentityPartsSeparator <- "-"
store.Initialize() |> ignore

let persister = new Persister(store)
let query = new Query(store)

//
let savePeriod (p : StockCheck.Model.Period) =
    persister.Save(p)

let period = query.GetModelPeriods |> Seq.filter(fun p -> p.Name = "April/May 2014") |> Seq.head

let updateSalesItem (s : StockCheck.Model.SalesItem) =
    let n = match s.ItemName.LedgerCode with
            | "1000" -> { s with SalesUnitType = Pint }
            | "1005" -> { s with SalesUnitType = Pint }
            | "1020" -> { s with SalesUnitType = Wine }
            | "1030" -> { s with SalesUnitType = Spirit }
            | _ -> { s with SalesUnitType = Unit }

    persister.Save(n)

[<Test>]
[<Ignore>]
let ``Update SalesItem price`` () =
    let p = period
    let items = p.Items |> Seq.filter(fun i -> i.SalesItem.ItemName.Name.IndexOf("Desperado") > -1)

    items 
        |> Seq.map(fun i -> { i with SalesItem = { i.SalesItem with SalesPrice = money 3.1M } })
        |> ignore

    let items2 = p.Items |> Seq.filter(fun i -> i.SalesItem.ItemName.Name.IndexOf("Desperado") > -1)

    savePeriod p

[<Test>]
[<Ignore>]
let ``Update SalesItem unit type`` () =
    let s = query.GetModelSalesItems
    s |> Seq.iter updateSalesItem