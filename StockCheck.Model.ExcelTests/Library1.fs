module StockCheck.Model.ExcelTests

open NUnit.Framework
open FsUnit
open OfficeOpenXml
open System.IO
open System.Xml
open StockCheck.Model

let GetSI (range : ExcelRange) =
    let salesItem = new SalesItem()
    salesItem.LedgerCode <- (Seq.nth 0 range).Value.ToString()
    salesItem.Name <- (Seq.nth 1 range).Value.ToString()
    salesItem.ContainerSize <- float ((Seq.nth 2 range).Value.ToString())
    salesItem.UnitOfSale <- float ((Seq.nth 3 range).Value.ToString())
    salesItem.CostPerContainer <- decimal ((Seq.nth 5 range).Value.ToString())
    salesItem.SalesPrice <- decimal ((Seq.nth 7 range).Value.ToString())
    salesItem.TaxRate <- float ((Seq.nth 9 range).Value.ToString())
    salesItem

let SelectRow (sheet : ExcelWorksheet) (i : int) =
    let rangeString = String.concat "" ["A"; i.ToString(); ":K"; i.ToString()]
    sheet.Select(rangeString)
    sheet.SelectedRange

let file = new FileInfo(@"C:\Users\g_wilson\Desktop\GBS.xlsx")
let package = new ExcelPackage(file)
let cat = package.Workbook.Worksheets 
let sheets = cat |> Seq.filter (fun a -> a.Name = "Catalogue")
let sheet = sheets |> Seq.head

let selector i = SelectRow sheet i

let rows = [3..259]

let items = 
    rows 
    |> List.map (fun i -> (selector i)) 
    |> List.filter (fun r -> (Seq.nth 1 r).Text.ToString() <> "")
    |> List.map (fun r -> GetSI r)

[<Test>]
[<Ignore>]
let ``sheets name should be Catalogue`` () =
    Seq.length sheets |> should equal 1

[<Test>]
[<Ignore>]
let ``should get all of our stock items`` () =
    Seq.length items |> should equal (rows.Length - 12)
