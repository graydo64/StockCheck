module StockCheck.Model.ExportUtils

open NUnit.Framework
open FsUnit
open OfficeOpenXml
open System.IO
open System.Linq
open System.Xml
open StockCheck.Model
open StockCheck.Repository
open Raven.Client
open Raven.Client.Document

let file = 
    let p = @"C:\Users\graeme\Desktop\stock.xlsx"
    let fileInfo = new FileInfo(p)
    if fileInfo.Exists then 
        fileInfo.Delete()
        new FileInfo(p)
    else
        fileInfo

let package = new ExcelPackage(file)
let cat = package.Workbook.Worksheets.Add("Catalogue")
let ws = package.Workbook.Worksheets.Add("Working Sheet")
let clo = package.Workbook.Worksheets.Add("Value of Closing Stock")

let store = new DocumentStore()
store.Url <- "http://localhost:8880/"
store.Conventions.IdentityPartsSeparator <- "-"
store.Initialize() |> ignore

let persister = new Persister(store)
let query = new Query(store)

let startingRow i = i + 3
let datesRow = 1
let headerRow = 2

let catColumn = 1
let itemColumn = 2
let contColumn = 3

let writeCloHeader (ws : ExcelWorksheet) =
    ws.SetValue(headerRow, catColumn, "Category")
    ws.SetValue(headerRow, itemColumn, "Item")
    ws.SetValue(headerRow, contColumn, "Container")
    ws.SetValue(headerRow, 4, "Closing on hand")
    ws.SetValue(headerRow, 5, "At Cost Ex")
    ws.SetValue(headerRow, 6, "At Sales Inc")
    ws.SetValue(headerRow, 7, "At Sales Ex")

let writeCatHeader (ws : ExcelWorksheet) =
    ws.SetValue(headerRow, catColumn, "Category")
    ws.SetValue(headerRow, itemColumn, "Item")
    ws.SetValue(headerRow, contColumn, "Container")
    ws.SetValue(headerRow, 4, "Units/Container Unit")
    ws.SetValue(headerRow, 5, "Cost/Container Ex")
    ws.SetValue(headerRow, 6, "Cost/Unit")
    ws.SetValue(headerRow, 7, "Sales Price Inc")
    ws.SetValue(headerRow, 8, "Sales Price Ex")
    ws.SetValue(headerRow, 9, "VAT Rate")
    ws.SetValue(headerRow, 10, "Ideal GP")

let writeWsHeader (ws : ExcelWorksheet) =
    ws.SetValue(headerRow, catColumn, "Category")
    ws.SetValue(headerRow, itemColumn, "Item")
    ws.SetValue(headerRow, contColumn, "Container")
    ws.SetValue(headerRow, 4, "Opening")
    ws.SetValue(headerRow, 5, "Containers Received")
    ws.SetValue(headerRow, 6, "Total Units")
    ws.SetValue(headerRow, 7, "Closing")
    ws.SetValue(headerRow, 8, "Sales")
    ws.SetValue(headerRow, 9, "Purchases Ex")
    ws.SetValue(headerRow, 10, "Purchases Inc")
    ws.SetValue(headerRow, 11, "Purchases Total")
    ws.SetValue(headerRow, 12, "Sales Inc")
    ws.SetValue(headerRow, 13, "Sales Ex")
    ws.SetValue(headerRow, 14, "Cost of Sales")
    ws.SetValue(headerRow, 15, "Profit")
    ws.SetValue(headerRow, 16, "Sales/Day")
    ws.SetValue(headerRow, 17, "Days on Hand")

let writeSalesItem i (si : StockCheck.Model.SalesItem) (ws : ExcelWorksheet) =
    let rowNo = startingRow i
    ws.SetValue(rowNo, catColumn, si.LedgerCode)
    ws.SetValue(rowNo, itemColumn, si.Name)
    ws.SetValue(rowNo, contColumn, si.ContainerSize)
    ws.SetValue(rowNo, 4, si.SalesUnitsPerContainerUnit)
    ws.SetValue(rowNo, 5, si.CostPerContainer)
    ws.SetValue(rowNo, 6, si.CostPerUnitOfSale)
    ws.SetValue(rowNo, 7, si.SalesPrice)
    ws.SetValue(rowNo, 8, StockCheck.Model.Utils.LessTax si.TaxRate si.SalesPrice)
    ws.SetValue(rowNo, 9, si.TaxRate)
    ws.SetValue(rowNo, 10, si.IdealGP)

let writeClosingItem i (p : StockCheck.Model.myPeriod) (pi : StockCheck.Model.PeriodItem) (ws : ExcelWorksheet) =
    let rowNo = startingRow i
    ws.SetValue(rowNo, catColumn, pi.SalesItem.LedgerCode)
    ws.SetValue(rowNo, itemColumn, pi.SalesItem.Name)
    ws.SetValue(rowNo, contColumn, pi.SalesItem.ContainerSize)
    ws.SetValue(rowNo, 4, pi.ClosingStock)
    ws.SetValue(rowNo, 5, pi.ClosingValueCostEx)
    ws.SetValue(rowNo, 6, pi.ClosingValueSalesInc)
    ws.SetValue(rowNo, 7, pi.ClosingValueSalesEx)
    pi

let writePeriodItem i (p : StockCheck.Model.myPeriod) (pi : StockCheck.Model.PeriodItem) (ws : ExcelWorksheet) =
    let rowNo = startingRow i
    ws.SetValue(rowNo, catColumn, pi.SalesItem.LedgerCode)
    ws.SetValue(rowNo, itemColumn, pi.SalesItem.Name)
    ws.SetValue(rowNo, contColumn, pi.SalesItem.ContainerSize)
    ws.SetValue(rowNo, 4, pi.OpeningStock)
    ws.SetValue(rowNo, 5, pi.ContainersReceived)
    ws.SetValue(rowNo, 6, pi.TotalUnits)
    ws.SetValue(rowNo, 7, pi.ClosingStock)
    ws.SetValue(rowNo, 8, pi.Sales)
    ws.SetValue(rowNo, 9, pi.PurchasesEx)
    ws.SetValue(rowNo, 10, pi.PurchasesInc)
    ws.SetValue(rowNo, 11, pi.PurchasesTotal)
    ws.SetValue(rowNo, 12, pi.SalesInc)
    ws.SetValue(rowNo, 13, pi.SalesEx)
    ws.SetValue(rowNo, 14, pi.CostOfSalesEx)
    ws.SetValue(rowNo, 15, pi.Profit)
    ws.SetValue(rowNo, 16, pi.SalesPerDay(p.StartOfPeriod, p.EndOfPeriod))
    ws.SetValue(rowNo, 17, pi.DaysOnHand(p.StartOfPeriod, p.EndOfPeriod))
    pi

let compareSalesItems (si1 : StockCheck.Model.SalesItem) (si2 : StockCheck.Model.SalesItem) =
    if si1.LedgerCode < si2.LedgerCode then -1 else
    if si1.LedgerCode > si2.LedgerCode then 1 else
    if si1.Name < si2.Name then -1 else
    if si1.Name > si2.Name then 1 else
    if si1.ContainerSize < si2.ContainerSize then -1 else
    if si1.ContainerSize > si2.ContainerSize then 1 else
    0

let comparePeriodItems (pi1 : StockCheck.Model.PeriodItem) (pi2 : StockCheck.Model.PeriodItem) =
    compareSalesItems pi1.SalesItem pi2.SalesItem

let periodBase = query.GetModelPeriods |> Seq.filter(fun p -> p.Name = "April/May 2014") |> Seq.head
let period = query.GetModelPeriodById periodBase.Id

let curFormat = "\£#,##0.00"
let pcFormat = "0.0%"
let dateFormat = "dd/mm/yyyy"

[<Test>]
[<Ignore>]
let ``Create Export Workbook`` () =
    writeCatHeader cat
    writeWsHeader ws
    writeCloHeader clo
    period.Items
    |> Seq.toList
    |> List.sortWith comparePeriodItems
    |> List.mapi(fun i si -> writePeriodItem i period si ws)
    |> List.mapi(fun i si -> writeClosingItem i period si clo)
    |> List.iteri(fun i pi -> writeSalesItem i pi.SalesItem cat)

    let scf (sh : ExcelWorksheet) format cols =
        cols |> Seq.iter(fun i -> sh.Column(i).Style.Numberformat.Format <- format)

    let sdf (r : ExcelRange) =
        let x = r.Style
        x.Font.Bold <- true
        x.Numberformat.Format <- dateFormat

    ws.View.FreezePanes(headerRow + 1, 4)
    ws.SetValue(datesRow, 4, period.StartOfPeriod)
    sdf ws.Cells.[datesRow, 4]
    ws.SetValue(datesRow, 7, period.EndOfPeriod)
    sdf ws.Cells.[datesRow, 7]

    ws.Cells.AutoFitColumns()
    scf ws curFormat {9..15}

    cat.View.FreezePanes(headerRow + 1, 4)
    cat.Cells.AutoFitColumns()
    scf cat curFormat {5..8}
    scf cat pcFormat {9..10}

    clo.SetValue(datesRow, 4, period.EndOfPeriod)
    sdf clo.Cells.[datesRow, 4]
    clo.View.FreezePanes(headerRow + 1, 4)
    clo.Cells.AutoFitColumns()
    scf clo curFormat {5..7}

    let sumCol (sh : ExcelWorksheet) sumRow col =
        let range = ws.Cells.[headerRow + 1, col, sumRow - 1, col]
        sh.Cells.[sumRow, col].Formula <- System.String.Format("SUM({0})", range.Address)
        
    let sumRow = period.Items.Count + headerRow + 1

    sumCol ws sumRow 12
    sumCol ws sumRow 13
    sumCol ws sumRow 14
    sumCol clo sumRow 5
    sumCol clo sumRow 6
    sumCol clo sumRow 7

    package.Workbook.Properties.Title <- "Stock Period " + period.Name
    package.Workbook.Properties.Company <- "Golden Ball Co-operative Limited"
    package.Save()
