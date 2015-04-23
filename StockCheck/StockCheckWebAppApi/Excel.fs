namespace FsWeb.Controllers

open System
open System.IO
open StockCheck.Model.Business
open StockCheck.Model.Factory
open OfficeOpenXml

module Excel =

    type ilFacade = {
        LedgerCode : string
        Name : string
        ContainerSize : float
        InvoiceNumber : string
        DeliveryDate : DateTime
        Qty : float
        AmountEx : decimal
        AmountInc : decimal
    }

    let NewFile() =
        let t = Environment.GetEnvironmentVariable("temp")
        let p = Path.Combine(t, "stock.xlsx")
        let f = new FileInfo(p)
        if f.Exists then 
            f.Delete()
            new FileInfo(p)
        else
            f

    let Export (period : StockCheck.Model.Period) (invoices : StockCheck.Model.Invoice seq) =
        let f = NewFile()
        use package = new ExcelPackage(f)
        let cat = package.Workbook.Worksheets.Add("Catalogue")
        let gr = package.Workbook.Worksheets.Add("Goods Received")
        let ws = package.Workbook.Worksheets.Add("Working Sheet")
        let clo = package.Workbook.Worksheets.Add("Value of Closing Stock")

        let rowOffset i = i + 3
        let datesRow = 1
        let headerRow = 2

        let lcCol = 1
        let siCol = 2
        let csCol = 3

        let curFormat = "\£#,##0.00"
        let pcFormat = "0.0%"
        let dateFormat = "dd/mm/yyyy"

        let writeGenericHeader (ws : ExcelWorksheet) =
            ws.SetValue(headerRow, lcCol, "Category")
            ws.SetValue(headerRow, siCol, "Item")
            ws.SetValue(headerRow, csCol, "Container")

        let writeCloHeader (ws : ExcelWorksheet) =
            ws.SetValue(headerRow, 4, "Closing on hand")
            ws.SetValue(headerRow, 5, "At Cost Ex")
            ws.SetValue(headerRow, 6, "At Sales Inc")
            ws.SetValue(headerRow, 7, "At Sales Ex")

        let writeGrHeader (ws : ExcelWorksheet) =
            ws.SetValue(headerRow, 4, "Invoice No")
            ws.SetValue(headerRow, 5, "Delivery Date")
            ws.SetValue(headerRow, 6, "Qty")
            ws.SetValue(headerRow, 7, "Total Ex")
            ws.SetValue(headerRow, 8, "Total Inc")
            ws.SetValue(headerRow, 9, "Week Ending")

        let writeCatHeader (ws : ExcelWorksheet) =
            ws.SetValue(headerRow, 4, "Units/Container Unit")
            ws.SetValue(headerRow, 5, "Cost/Container Ex")
            ws.SetValue(headerRow, 6, "Cost/Unit")
            ws.SetValue(headerRow, 7, "Sales Price Inc")
            ws.SetValue(headerRow, 8, "Sales Price Ex")
            ws.SetValue(headerRow, 9, "VAT Rate")
            ws.SetValue(headerRow, 10, "Ideal GP")

        let writeWsHeader (ws : ExcelWorksheet) =
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
            let rowNo = rowOffset i
            let sii = getSalesItemInfo si
            ws.SetValue(rowNo, lcCol, si.ItemName.LedgerCode)
            ws.SetValue(rowNo, siCol, si.ItemName.Name)
            ws.SetValue(rowNo, csCol, si.ItemName.ContainerSize)
            ws.SetValue(rowNo, 4, sii.SalesUnitsPerContainerUnit)
            ws.SetValue(rowNo, 5, si.CostPerContainer)
            ws.SetValue(rowNo, 6, sii.CostPerUnitOfSale)
            ws.SetValue(rowNo, 7, si.SalesPrice)
            ws.SetValue(rowNo, 8, StockCheck.Model.Business.lessTax si.TaxRate si.SalesPrice)
            ws.SetValue(rowNo, 9, si.TaxRate)
            ws.SetValue(rowNo, 10, sii.IdealGP)

        let writeClosingItem i (p : StockCheck.Model.Period) (pi : StockCheck.Model.PeriodItem) (ws : ExcelWorksheet) =
            let rowNo = rowOffset i
            let pii = getPeriodItemInfo pi
            ws.SetValue(rowNo, lcCol, pi.SalesItem.ItemName.LedgerCode)
            ws.SetValue(rowNo, siCol, pi.SalesItem.ItemName.Name)
            ws.SetValue(rowNo, csCol, pi.SalesItem.ItemName.ContainerSize)
            ws.SetValue(rowNo, 4, pi.ClosingStock)
            ws.SetValue(rowNo, 5, pii.ClosingValueCostEx)
            ws.SetValue(rowNo, 6, pii.ClosingValueSalesInc)
            ws.SetValue(rowNo, 7, pii.ClosingValueSalesEx)
            pi

        let writeGoodsIn i f (ws : ExcelWorksheet) =
            let rowNo = rowOffset i
            let wer = ws.Cells.[rowNo, 9]
            let ddr = ws.Cells.[rowNo, 5]
            ws.SetValue(rowNo, lcCol, f.LedgerCode)
            ws.SetValue(rowNo, siCol, f.Name)
            ws.SetValue(rowNo, csCol, f.ContainerSize)
            ws.SetValue(rowNo, 4, f.InvoiceNumber)
            ws.SetValue(rowNo, 5, f.DeliveryDate.ToLocalTime())
            ws.SetValue(rowNo, 6, f.Qty)
            ws.SetValue(rowNo, 7, f.AmountEx)
            ws.SetValue(rowNo, 8, f.AmountInc)
            wer.Formula <- System.String.Format("{0}+(7-WEEKDAY({0},2))", ddr.Address)
            

        let writePeriodItem i (p : StockCheck.Model.Period) (pi : StockCheck.Model.PeriodItem) (ws : ExcelWorksheet) =
            let rowNo = rowOffset i
            let pii = getPeriodItemInfo pi
            ws.SetValue(rowNo, lcCol, pi.SalesItem.ItemName.LedgerCode)
            ws.SetValue(rowNo, siCol, pi.SalesItem.ItemName.Name)
            ws.SetValue(rowNo, csCol, pi.SalesItem.ItemName.ContainerSize)
            ws.SetValue(rowNo, 4, pi.OpeningStock)
            ws.SetValue(rowNo, 5, pii.ContainersReceived)
            ws.SetValue(rowNo, 6, pii.TotalUnits)
            ws.SetValue(rowNo, 7, pi.ClosingStock)
            ws.SetValue(rowNo, 8, pii.Sales)
            ws.SetValue(rowNo, 9, pii.PurchasesEx)
            ws.SetValue(rowNo, 10, pii.PurchasesInc)
            ws.SetValue(rowNo, 11, pii.PurchasesTotal)
            ws.SetValue(rowNo, 12, pii.SalesInc)
            ws.SetValue(rowNo, 13, pii.SalesEx)
            ws.SetValue(rowNo, 14, pii.CostOfSalesEx)
            ws.SetValue(rowNo, 15, pii.MarkUp)
            ws.SetValue(rowNo, 16, salesPerDay p.StartOfPeriod p.EndOfPeriod pii)
            ws.SetValue(rowNo, 17, daysOnHand p.StartOfPeriod p.EndOfPeriod pi pii)
            pi

        let compareSalesItems (si1 : StockCheck.Model.SalesItem) (si2 : StockCheck.Model.SalesItem) =
            if si1 < si2 then -1 else
            if si1 > si2 then 1 else
            0

        let comparePeriodItems (pi1 : StockCheck.Model.PeriodItem) (pi2 : StockCheck.Model.PeriodItem) =
            compareSalesItems pi1.SalesItem pi2.SalesItem

        let init (sh : ExcelWorksheet) =
            sh.View.FreezePanes(headerRow + 1, 4)
            sh.Cells.AutoFitColumns()
            sh.Row(datesRow).Style.Font.Bold <- true
            
        let scf (sh : ExcelWorksheet) format cols =
            cols |> Seq.iter(fun i -> sh.Column(i).Style.Numberformat.Format <- format)

        let sdf (r : ExcelRange) =
            let x = r.Style
            x.Numberformat.Format <- dateFormat
            r

        let writeData = 
            period.Items
            |> Seq.toList
            |> List.sortWith comparePeriodItems
            |> List.mapi(fun i si -> writePeriodItem i period si ws)
            |> List.mapi(fun i si -> writeClosingItem i period si clo)
            |> List.iteri(fun i pi -> writeSalesItem i pi.SalesItem cat)

        let getGoodsIn n d (il : StockCheck.Model.InvoiceLine) = 
            {
                ilFacade.LedgerCode = il.SalesItem.ItemName.LedgerCode
                Name = il.SalesItem.ItemName.Name
                ContainerSize = il.SalesItem.ItemName.ContainerSize
                InvoiceNumber = n
                DeliveryDate = d
                Qty = il.Quantity
                AmountEx = il.InvoicedAmountEx / 1.0M<StockCheck.Model.money>
                AmountInc = il.InvoicedAmountInc / 1.0M<StockCheck.Model.money>
            }

        let lines n d il = il |> Seq.map(fun i -> getGoodsIn n d i)

        let writeGoodsReceived =
            invoices
            |> Seq.collect(fun i -> lines i.InvoiceNumber i.DeliveryDate i.InvoiceLines)
            |> Seq.toList
            |> List.mapi(fun i f -> writeGoodsIn i f gr)

        let sheets = [ws; gr; cat; clo]

        sheets |> Seq.iter writeGenericHeader
        writeCatHeader cat
        writeGrHeader gr
        writeWsHeader ws
        writeCloHeader clo
        writeData
        writeGoodsReceived |> ignore

        ws.Cells.[datesRow, 4] |> sdf |> fun c -> c.Value <- period.StartOfPeriod.ToLocalTime()
        ws.Cells.[datesRow, 7] |> sdf |> fun c -> c.Value <- period.EndOfPeriod.ToLocalTime()

        scf ws curFormat {9..15}

        scf cat curFormat {5..8}
        scf cat pcFormat {9..10}

        clo.Cells.[datesRow, 4] |> sdf |> fun c -> c.Value <- period.EndOfPeriod.ToLocalTime()
        scf clo curFormat {5..7}

        gr.Column(5).Style.Numberformat.Format <- dateFormat
        gr.Column(9).Style.Numberformat.Format <- dateFormat
        scf gr curFormat {7..8}
        let sumCol (sh : ExcelWorksheet) sumRow col =
            let range = ws.Cells.[headerRow + 1, col, sumRow - 1, col]
            sh.Cells.[sumRow, col].Formula <- System.String.Format("SUM({0})", range.Address)
        
        let sumRow = (Seq.length period.Items) + headerRow + 1

        sumCol ws sumRow 12
        sumCol ws sumRow 13
        sumCol ws sumRow 14
        sumCol clo sumRow 5
        sumCol clo sumRow 6
        sumCol clo sumRow 7

        sheets |> Seq.iter init

        package.Workbook.Properties.Title <- "Stock Period " + period.Name
        package.Workbook.Properties.Company <- "Golden Ball Co-operative Limited"
        package.Save()
        f
