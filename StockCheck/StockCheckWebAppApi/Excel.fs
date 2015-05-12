namespace FsWeb.Controllers

open System
open System.IO
open OfficeOpenXml

module Excel =

    type ilFacade = {
        ProductCode : string
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

        let pcCol = 1
        let lcCol = 2
        let siCol = 3
        let csCol = 4

        let curFormat = "\£#,##0.00"
        let pcFormat = "0.0%"
        let dateFormat = "dd/mm/yyyy"

        let genHeadings = ["Prd Code"; "Category"; "Item"; "Container"]
        let cloHeadings = ["Closing on hand"; "At Cost Ex"; "At Sales Inc"; "At Sales Ex"]
        let grHeadings = ["Invoice No"; "Delivery Date"; "Qty"; "Total Ex"; "Total Inc"; "Week Ending"]
        let catHeadings = ["Units/Container Unit"; "Cost/Container Ex"; "Cost/Unit"; "Sales Price Inc"; "Sales Price Ex"; "VAT Rate"; "Ideal GP"]
        let wsHeadings = ["Opening"; "Containers Received"; "Total Units"; "Closing"; "Sales"; "Purchases Ex"; "Purchases Inc"; "Purchases Total"; "Sales Inc"; "Sales Ex"; "Cost of Sales"; "Profit"; "Sales/Day"; "Days on Hand"]

        let writeHeadings ((ws : ExcelWorksheet), headings) =
            Seq.append genHeadings headings
            |> Seq.iteri(fun i h -> ws.SetValue(headerRow, i + 1, h))

        let writeCell (s : ExcelWorksheet) r c v =
            s.SetValue(r, c, v)  

        let writeRowHeading (s : ExcelWorksheet) r (si : StockCheck.Model.SalesItem) =  
            let write c v = writeCell s r c v
            write pcCol si.ProductCode
            write lcCol si.LedgerCode
            write siCol si.Name
            write csCol si.ContainerSize

        let writeSalesItem i (si : StockCheck.Model.SalesItem) (ws : ExcelWorksheet) =
            let rowNo = rowOffset i
            let write c v = writeCell ws rowNo c v
            writeRowHeading ws rowNo si
            write (csCol + 1) si.SalesUnitsPerContainerUnit
            write (csCol + 2) si.CostPerContainer
            write (csCol + 3) si.CostPerUnitOfSale
            write (csCol + 4) si.SalesPrice
            write (csCol + 5) (StockCheck.Model.Utils.LessTax si.TaxRate si.SalesPrice)
            write (csCol + 6) si.TaxRate
            write (csCol + 7) si.IdealGP

        let writeClosingItem i (pi : StockCheck.Model.PeriodItem) (ws : ExcelWorksheet) =
            let rowNo = rowOffset i
            let write c v = writeCell ws rowNo c v
            writeRowHeading ws rowNo pi.SalesItem
            write (csCol + 1) pi.ClosingStock
            write (csCol + 2) pi.ClosingValueCostEx
            write (csCol + 3) pi.ClosingValueSalesInc
            write (csCol + 4) pi.ClosingValueSalesEx
            pi

        let writeGoodsIn i f (ws : ExcelWorksheet) =
            let rowNo = rowOffset i
            let wer = ws.Cells.[rowNo, (csCol + 5 + 1)]
            let ddr = ws.Cells.[rowNo, (csCol + 2)]
            let write c v = writeCell ws rowNo c v
            write pcCol f.ProductCode
            write lcCol f.LedgerCode
            write siCol f.Name
            write csCol f.ContainerSize
            write (csCol + 1) f.InvoiceNumber
            write (csCol + 2) (f.DeliveryDate.ToLocalTime())
            write (csCol + 3) f.Qty
            write (csCol + 4) f.AmountEx
            write (csCol + 5) f.AmountInc
            wer.Formula <- System.String.Format("{0}+(7-WEEKDAY({0},2))", ddr.Address)
        
        let writePeriodItem i (p : StockCheck.Model.Period) (pi : StockCheck.Model.PeriodItem) (ws : ExcelWorksheet) =
            let rowNo = rowOffset i
            let write c v = writeCell ws rowNo c v
            writeRowHeading ws rowNo pi.SalesItem
            write (csCol + 1) pi.OpeningStock
            write (csCol + 2) pi.ContainersReceived
            write (csCol + 3) pi.TotalUnits
            write (csCol + 4) pi.ClosingStock
            write (csCol + 5) pi.Sales
            write (csCol + 6) pi.PurchasesEx
            write (csCol + 7) pi.PurchasesInc
            write (csCol + 8) pi.PurchasesTotal
            write (csCol + 9) pi.SalesInc
            write (csCol + 10) pi.SalesEx
            write (csCol + 11) pi.CostOfSalesEx
            write (csCol + 12) pi.Profit
            write (csCol + 13) (pi.SalesPerDay(p.StartOfPeriod, p.EndOfPeriod))
            write (csCol + 14) (pi.DaysOnHand(p.StartOfPeriod, p.EndOfPeriod))
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

        let init (sh : ExcelWorksheet) =
            sh.View.FreezePanes(headerRow + 1, 5)
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
            |> List.mapi(fun i si -> writeClosingItem i si clo)
            |> List.iteri(fun i pi -> writeSalesItem i pi.SalesItem cat)

        let getGoodsIn n d (il : StockCheck.Model.InvoiceLine) = 
            {
                ilFacade.ProductCode = il.SalesItem.ProductCode
                LedgerCode = il.SalesItem.LedgerCode
                Name = il.SalesItem.Name
                ContainerSize = il.SalesItem.ContainerSize
                InvoiceNumber = n
                DeliveryDate = d
                Qty = il.Quantity
                AmountEx = il.InvoicedAmountEx
                AmountInc = il.InvoicedAmountInc
            }

        let lines n d il = il |> Seq.map(fun i -> getGoodsIn n d i)

        let writeGoodsReceived =
            invoices
            |> Seq.collect(fun i -> lines i.InvoiceNumber i.DeliveryDate i.InvoiceLines)
            |> Seq.toList
            |> List.mapi(fun i f -> writeGoodsIn i f gr)

        let sheets = [ws; gr; cat; clo]

        let hds = [ (ws, wsHeadings); (gr, grHeadings); (cat, catHeadings); (clo, cloHeadings) ]

        hds
        |> Seq.iter writeHeadings
        writeData
        writeGoodsReceived |> ignore

        ws.Cells.[datesRow, 5] |> sdf |> fun c -> c.Value <- period.StartOfPeriod.ToLocalTime()
        ws.Cells.[datesRow, 8] |> sdf |> fun c -> c.Value <- period.EndOfPeriod.ToLocalTime()

        scf ws curFormat {10..16}

        scf cat curFormat {6..9}
        scf cat pcFormat {10..11}

        clo.Cells.[datesRow, 5] |> sdf |> fun c -> c.Value <- period.EndOfPeriod.ToLocalTime()
        scf clo curFormat {6..8}

        gr.Column(6).Style.Numberformat.Format <- dateFormat
        gr.Column(10).Style.Numberformat.Format <- dateFormat
        scf gr curFormat {8..9}
        let sumCol (sh : ExcelWorksheet) sumRow col =
            let range = ws.Cells.[headerRow + 1, col, sumRow - 1, col]
            sh.Cells.[sumRow, col].Formula <- System.String.Format("SUM({0})", range.Address)
        
        let sumRow = period.Items.Count + headerRow + 1

        sumCol ws sumRow 13
        sumCol ws sumRow 14
        sumCol ws sumRow 15
        sumCol clo sumRow 6
        sumCol clo sumRow 7
        sumCol clo sumRow 8

        sheets |> Seq.iter init

        package.Workbook.Properties.Title <- "Stock Period " + period.Name
        package.Workbook.Properties.Company <- "Golden Ball Co-operative Limited"
        package.Save()
        f
