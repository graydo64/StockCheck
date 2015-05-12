// Initialise the ProductCode field for all SalesItems based on their ordinal position when sorted on LedgerCode, Name and ContainerSize.

#r @"C:\Users\graeme\Documents\GitHub\StockCheck\StockCheck.Repository\bin\Debug\StockCheck.Repository.dll"
#load "Common.fsx"

open StockCheck.Repository
open System
open System.Linq
open Common

let s = Common.session.Query<StockCheck.Repository.SalesItem>().Take(1024).AsEnumerable()

s
|> Seq.sortBy(fun i -> i.LedgerCode, i.Name, i.ContainerSize)
|> Seq.mapi(fun i s -> 
                s.ProductCode <- (i + 1).ToString()
                s )
|> Seq.iter(fun i -> Common.session.Store(i))

//Common.session.SaveChanges()


