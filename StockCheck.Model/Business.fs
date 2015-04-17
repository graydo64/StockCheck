namespace StockCheck.Model

open StockCheck.Model.Conv

module Business =

    let markUp sale cost = sale - cost

    let grossProfit (sale : decimal<money>) (cost : decimal<money>) =
        match sale with
        | 0M<money> -> 0.<percentage>
        | _ -> 
            let markUpFloat = float ((markUp sale cost)/1.0M<money>)
            let saleFloat = float (sale / 1.0M<money>)
            percentage (markUpFloat / saleFloat)

    let lessTax (rate : float<percentage>) (price : decimal<money>) =
        let denom = decimal ((1.<percentage> + rate) / 1.<percentage>)
        money (price / money denom)

    let costPerUnitOfSale (costPerContainer : decimal<money>) (containerSize : float) (salesUnitsPerContainerUnit : float) =
        let salesUnits = containerSize * salesUnitsPerContainerUnit
        let salesUnitsAsDecimal = decimal salesUnits
        let costAsDecimal = costPerContainer / 1.0M<money>
        match costAsDecimal with
        | 0.M -> money 0.M
        | _ -> money costAsDecimal/salesUnitsAsDecimal

    let salesUnitsPerContainerUnit (s : mySalesItem) =
        match s.SalesUnitType with
        | Pint -> float ptPerGal
        | Unit -> 1.0
        | Spirit -> float shotsPerLtr
        | Fortified -> float doublesPerLtr
        | Wine -> float wineGlassPerLtr
        | Other -> s.OtherSalesUnit

    let totalUnits (si : mySalesItem) (cr : float) (cs : float) =
        match si.SalesUnitType with
        | Pint | Unit | Other -> cr * cs
        | Spirit | Fortified | Wine -> cr

    let private valueOfQuantity qty unit size (ppUnit : decimal<money>)=
        money (decimal (qty * unit * size * float (ppUnit / 1.0M<money>)))

    let valueOfQuantityT t unit size qty ppUnit =
        match t with
        | Pint | Unit -> valueOfQuantity qty unit 1.0 ppUnit
        | Spirit | Fortified | Wine | Other -> valueOfQuantity qty unit size ppUnit



module Factory =
    open Business

    let getSalesItemInfo (s : mySalesItem) = 
        let sucu = (salesUnitsPerContainerUnit s)
        let spx = lessTax (s.TaxRate * 1.) s.SalesPrice
        let cpus = costPerUnitOfSale s.CostPerContainer s.ItemName.ContainerSize sucu
        let mu = markUp spx cpus
        let igp = grossProfit spx cpus
        { MarkUp = mu; CostPerUnitOfSale = cpus; IdealGP = igp; SalesUnitsPerContainerUnit = sucu; SalesPriceEx = spx }

    let getPeriodItemInfo ((p : myPeriodItem),(i : List<myItemReceived>),(sii : SalesItemInfo)) = 
        let si = p.SalesItem
        let lessTax = Business.lessTax si.TaxRate
        let valueOfQuantity = valueOfQuantityT si.SalesUnitType sii.SalesUnitsPerContainerUnit si.ItemName.ContainerSize 
        let cr = i |> Seq.sumBy (fun i -> i.Quantity)
        let tu = totalUnits p.SalesItem cr p.SalesItem.ItemName.ContainerSize
        let st = Utils.Round2 (p.OpeningStock + tu - p.ClosingStock)
        let pex = i |> Seq.sumBy (fun i -> i.InvoicedAmountEx)
        let pin = i |> Seq.sumBy (fun i -> i.InvoicedAmountInc)
        let pt = pex + lessTax pin
        let sinc = valueOfQuantity st si.SalesPrice
        let sexc = lessTax sinc
        let contRec = if cr > 0. then Some(cr) else None
        let salesCost = 
            let c = decimal st * si.CostPerContainer
            match si.SalesUnitType with
            | salesUnitType.Spirit | salesUnitType.Fortified | salesUnitType.Other ->
                c
            | _ ->
                c / decimal si.ItemName.ContainerSize

        let cos =         
            match contRec with
            | Some(c) -> 
                money (decimal ((float (pt / 1.0M<money>) / tu) * st))
            | None -> 
                salesCost

        let cvc = valueOfQuantity p.ClosingStock sii.CostPerUnitOfSale
        let cvsin = valueOfQuantity p.ClosingStock si.SalesPrice
        let cvsex = valueOfQuantity p.ClosingStock (lessTax cvsin)

        { 
            ContainersReceived = cr;
            TotalUnits = tu;
            Sales = st;
            ContainersSold = st / p.SalesItem.ItemName.ContainerSize;
            PurchasesEx = pex;
            PurchasesInc = pin;
            PurchasesTotal = pt;
            SalesInc = sinc;
            SalesEx = sexc;
            CostOfSalesEx = cos;
            MarkUp = sexc - cos;
            ClosingValueCostEx = cvc;
            ClosingValueSalesInc = cvsin;
            ClosingValueSalesEx = cvsex
         }

    let getPeriodInfo (p : myPeriod) = 
        let s = p.Items |> Seq.sumBy(fun i -> i.SalesEx)
        let cvsi = p.Items |> Seq.sumBy(fun i -> i.ClosingValueSalesInc)
        let cvse = p.Items |> Seq.sumBy(fun i -> i.ClosingValueSalesEx)
        let cvce = p.Items |> Seq.sumBy(fun i -> i.ClosingValueCostEx)
        {
            SalesEx = money s;
            ClosingValueSalesInc = money cvsi;
            ClosingValueSalesEx = money cvse;
            ClosingValueCostEx = money cvce;
        }

