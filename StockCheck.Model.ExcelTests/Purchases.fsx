#r @"C:\Users\graeme\Documents\GitHub\StockCheck\StockCheck.Repository\bin\Debug\StockCheck.Repository.dll"
#load "Common.fsx"

// Script to list out all items received by date range and ledger code.

open StockCheck.Repository
open System
open System.Linq
open Common
let startDate = new DateTime(2014, 4, 1)
let endDate = new DateTime(2015, 3, 31, 23, 59, 59)
let c gp s = s * (1. - gp)

let p = Common.session.Query<StockCheck.Repository.Invoice>().Take(1024).Where(fun i -> i.DeliveryDate >= startDate && i.DeliveryDate <= endDate).OrderBy(fun i -> i.DeliveryDate)

// Print a filtered list of items received.
p 
|> Seq.collect(fun i -> i.InvoiceLines) 
|> Seq.filter(fun i -> i.SalesItem.LedgerCode = "1000" || i.SalesItem.LedgerCode = "1005")
|> Seq.iter(fun i -> printfn "%s, %s, %f, %f, %f" i.SalesItem.LedgerCode i.SalesItem.Name i.SalesItem.ContainerSize i.Quantity i.InvoicedAmountEx)

// Sum invoiced amount for keg
let keg = p 
        |> Seq.collect(fun i -> i.InvoiceLines) 
        |> Seq.filter(fun i -> i.SalesItem.LedgerCode = "1000")
        |> Seq.sumBy(fun i -> i.InvoicedAmountEx)

printfn "%f" keg

let ko = c 0.45 (float keg)

printfn "%f" ko
printfn "%f" (keg - decimal ko)

// sum invoiced amount for cask.
let cask = p 
        |> Seq.collect(fun i -> i.InvoiceLines) 
        |> Seq.filter(fun i -> i.SalesItem.LedgerCode = "1005")
        |> Seq.sumBy(fun i -> i.InvoicedAmountEx)

printfn "%f" cask

let co = c 0.45 (float cask)

printfn "%f" co
printfn "%f" (cask - decimal co)
