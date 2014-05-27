﻿module StockCheck.Model.ExcelTests

open NUnit.Framework
open FsUnit
open OfficeOpenXml
open System.IO
open System.Xml
open StockCheck.Model
open StockCheck.Repository
open Raven.Client.Document

let GetSI (range : ExcelRange) =
    let costPerContainer =
        match (Seq.nth 5 range).Value with 
        | null -> "0" 
        | _ -> (Seq.nth 5 range).Value.ToString()
    let salesPrice =
        match (Seq.nth 7 range).Value with 
        | null -> "0" 
        | _ -> (Seq.nth 7 range).Value.ToString()
    let salesItem = new StockCheck.Model.SalesItem()
    salesItem.LedgerCode <- (Seq.nth 0 range).Value.ToString()
    salesItem.Name <- (Seq.nth 1 range).Value.ToString()
    salesItem.ContainerSize <- float ((Seq.nth 2 range).Value.ToString())
    salesItem.SalesUnitsPerContainerUnit <- float ((Seq.nth 3 range).Value.ToString())
    salesItem.CostPerContainer <- decimal (costPerContainer)
    salesItem.SalesPrice <- decimal (salesPrice)
    salesItem.TaxRate <- float ((Seq.nth 9 range).Value.ToString())
    salesItem

let SelectRow (sheet : ExcelWorksheet) (i : int) =
    let rangeString = String.concat "" ["A"; i.ToString(); ":K"; i.ToString()]
    sheet.Select(rangeString)
    sheet.SelectedRange

let file = new FileInfo(@"C:\Users\graeme\Downloads\Golden Ball Stock March 2014.xlsx")
let package = new ExcelPackage(file)
let cat = package.Workbook.Worksheets 
let sheets = cat |> Seq.filter (fun a -> a.Name = "Catalogue")
let sheet = sheets |> Seq.head

let selector i = SelectRow sheet i

let rows = [3..241]

let items = 
    rows 
    |> List.map (fun i -> (selector i)) 
    |> List.filter (fun r -> (Seq.nth 1 r).Text.ToString() <> "")
    |> List.map (fun r -> GetSI r)

let store = new DocumentStore()
store.Url <- "http://localhost:8880/"
store.Conventions.IdentityPartsSeparator <- "-"
store.Initialize() |> ignore

let persister = new Persister(store)
//
let saveSalesItem (i : StockCheck.Model.SalesItem) =
    persister.Save(i)

let saveOut = items |> List.map (fun i -> saveSalesItem i)

[<Test>]
[<Ignore>]
let ``sheets name should be Catalogue`` () =
    Seq.length sheets |> should equal 1

[<Test>]
[<Ignore>]
let ``should get all of our stock items`` () =
    Seq.length items |> should equal (rows.Length - 12)
