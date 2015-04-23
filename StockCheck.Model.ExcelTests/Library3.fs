module StockCheck.Model.ExportUtils

open NUnit.Framework
open FsUnit
open OfficeOpenXml
open System.IO
open System.Linq
open System.Xml
open StockCheck.Model
open StockCheck.Model.Business
open StockCheck.Model.Factory
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

let writeSalesItem i (si : StockCheck.Model.mySalesItem) (ws : ExcelWorksheet) =
    let rowNo = startingRow i
    let sii = getSalesItemInfo si
    ws.SetValue(rowNo, catColumn, si.ItemName.LedgerCode)
    ws.SetValue(rowNo, itemColumn, si.ItemName.Name)
    ws.SetValue(rowNo, contColumn, si.ItemName.ContainerSize)
    ws.SetValue(rowNo, 4, sii.SalesUnitsPerContainerUnit)
    ws.SetValue(rowNo, 5, si.CostPerContainer)
    ws.SetValue(rowNo, 6, sii.CostPerUnitOfSale)
    ws.SetValue(rowNo, 7, si.SalesPrice)
    ws.SetValue(rowNo, 8, StockCheck.Model.Business.lessTax si.TaxRate si.SalesPrice)
    ws.SetValue(rowNo, 9, si.TaxRate)
    ws.SetValue(rowNo, 10, sii.IdealGP)

let writeClosingItem i (p : StockCheck.Model.myPeriod) (pi : StockCheck.Model.myPeriodItem) (ws : ExcelWorksheet) =
    let rowNo = startingRow i
    let piInfo = getPeriodItemInfo (pi, pi.ItemsReceived, getSalesItemInfo pi.SalesItem)
    ws.SetValue(rowNo, catColumn, pi.SalesItem.ItemName.LedgerCode)
    ws.SetValue(rowNo, itemColumn, pi.SalesItem.ItemName.Name)
    ws.SetValue(rowNo, contColumn, pi.SalesItem.ItemName.ContainerSize)
    ws.SetValue(rowNo, 4, pi.ClosingStock)
    ws.SetValue(rowNo, 5, piInfo.ClosingValueCostEx)
    ws.SetValue(rowNo, 6, piInfo.ClosingValueSalesInc)
    ws.SetValue(rowNo, 7, piInfo.ClosingValueSalesEx)
    pi

let writePeriodItem i (p : StockCheck.Model.myPeriod) (pi : StockCheck.Model.myPeriodItem) (ws : ExcelWorksheet) =
    let rowNo = startingRow i
    let piInfo = getPeriodItemInfo (pi, pi.ItemsReceived, getSalesItemInfo pi.SalesItem)
    ws.SetValue(rowNo, catColumn, pi.SalesItem.ItemName.LedgerCode)
    ws.SetValue(rowNo, itemColumn, pi.SalesItem.ItemName.Name)
    ws.SetValue(rowNo, contColumn, pi.SalesItem.ItemName.ContainerSize)
    ws.SetValue(rowNo, 4, pi.OpeningStock)
    ws.SetValue(rowNo, 5, piInfo.ContainersReceived)
    ws.SetValue(rowNo, 6, piInfo.TotalUnits)
    ws.SetValue(rowNo, 7, pi.ClosingStock)
    ws.SetValue(rowNo, 8, piInfo.Sales)
    ws.SetValue(rowNo, 9, piInfo.PurchasesEx)
    ws.SetValue(rowNo, 10, piInfo.PurchasesInc)
    ws.SetValue(rowNo, 11, piInfo.PurchasesTotal)
    ws.SetValue(rowNo, 12, piInfo.SalesInc)
    ws.SetValue(rowNo, 13, piInfo.SalesEx)
    ws.SetValue(rowNo, 14, piInfo.CostOfSalesEx)
    ws.SetValue(rowNo, 15, piInfo.MarkUp)
    ws.SetValue(rowNo, 16, Business.salesPerDay p.StartOfPeriod p.EndOfPeriod piInfo)
    ws.SetValue(rowNo, 17, Business.daysOnHand p.StartOfPeriod p.EndOfPeriod pi piInfo)
    pi

let compareSalesItems (si1 : StockCheck.Model.mySalesItem) (si2 : StockCheck.Model.mySalesItem) =
    if si1 < si2 then -1 else
    if si1 > si2 then 1 else
    0

let comparePeriodItems (pi1 : StockCheck.Model.myPeriodItem) (pi2 : StockCheck.Model.myPeriodItem) =
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
        
    let sumRow = period.Items.Count() + headerRow + 1

    sumCol ws sumRow 12
    sumCol ws sumRow 13
    sumCol ws sumRow 14
    sumCol clo sumRow 5
    sumCol clo sumRow 6
    sumCol clo sumRow 7

    package.Workbook.Properties.Title <- "Stock Period " + period.Name
    package.Workbook.Properties.Company <- "Golden Ball Co-operative Limited"
    package.Save()
