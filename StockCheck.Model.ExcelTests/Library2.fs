module StockCheck.Model.ExcelTests2

open System
open NUnit.Framework
open FsUnit
open StockCheck.Model
open StockCheck.Repository

let period = new StockCheck.Model.Period()
period.Name <- "April 2013"
period.StartOfPeriod <- new DateTime(2013, 4, 4)
period.EndOfPeriod <- new DateTime(2013, 4, 28)

let query = StockCheck.Repository.Query("mongodb://localhost")
let salesItems = 
        [
        (query.GetModelSalesItem "Kronenbourg" "1000", 63., 30.)
        (query.GetModelSalesItem "Fosters" "1000", 39., 55.)
        (query.GetModelSalesItem "Guinness" "1000", 22., 9.)
        ]

let periodItems = salesItems |> List.map (fun (i, j, k) -> 
        let pi = new StockCheck.Model.PeriodItem(i)
        pi.OpeningStock <- j
        pi.ClosingStock <- k
        pi
        )
period.Items <- System.Collections.Generic.List<StockCheck.Model.PeriodItem> periodItems

let kro = period.Items |> Seq.filter (fun i -> i.SalesItem.Name = "Kronenbourg") |> Seq.head
kro.ReceiveItems (new DateTime(2013, 4, 14)) 2. (decimal 2 * kro.SalesItem.CostPerContainer) (decimal 0)
kro.ReceiveItems (new DateTime(2013, 4, 21)) 1. (decimal 1 * kro.SalesItem.CostPerContainer) (decimal 0)
kro.ReceiveItems (new DateTime(2013, 4, 28)) 1. (decimal 1 * kro.SalesItem.CostPerContainer) (decimal 0)

let fos = period.Items |> Seq.filter (fun i -> i.SalesItem.Name = "Fosters") |> Seq.head
fos.ReceiveItems (new DateTime(2013, 4, 14)) 2. (decimal 2 * fos.SalesItem.CostPerContainer) (decimal 0)
fos.ReceiveItems (new DateTime(2013, 4, 21)) 1. (decimal 1 * fos.SalesItem.CostPerContainer) (decimal 0)
fos.ReceiveItems (new DateTime(2013, 4, 28)) 3. (decimal 3 * fos.SalesItem.CostPerContainer) (decimal 0)

let gui = period.Items |> Seq.filter (fun i -> i.SalesItem.Name = "Guinness") |> Seq.head
gui.ReceiveItems (new DateTime(2013, 4, 14)) 1. (decimal 1 * gui.SalesItem.CostPerContainer) (decimal 0)
gui.ReceiveItems (new DateTime(2013, 4, 28)) 1. (decimal 1 * gui.SalesItem.CostPerContainer) (decimal 0)

let persister = new Persister("mongodb://localhost")
persister.Save(period)

[<Test>]
[<Ignore>]
let ``there should be three period items`` () =
    Seq.length period.Items |> should equal 3

