// Initialise the ProductCode field for all SalesItems embedded within a Period.

#r @"C:\Users\graeme\Documents\GitHub\StockCheck\StockCheck.Repository\bin\Debug\StockCheck.Repository.dll"
#load "Common.fsx"

open StockCheck.Repository
open System
open System.Linq
open Common

let p = Common.session.Query<StockCheck.Repository.Period>().Where(fun i -> i.Name = "March 2015").First()
let s = Common.session.Query<StockCheck.Repository.SalesItem>().Take(1024).AsEnumerable()
let si = s |> Seq.cache
let newCode (i : StockCheck.Repository.SalesItem) =
    si.Where(fun x -> x.Id = i.Id).First().ProductCode

p.Items
|> Seq.iter(fun x -> x.SalesItem.ProductCode <- (newCode x.SalesItem))
|> ignore

Common.session.Store(p) |> ignore
Common.session.SaveChanges() |> ignore


