namespace StockCheck.Model

open System
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

    let salesPerDay (startDate: DateTime) (endDate: DateTime) (pii: PeriodItemInfo) = pii.Sales / float (endDate.Subtract(startDate).Days + 1)

    let daysOnHand (startDate: DateTime)  (endDate: DateTime) (pi: myPeriodItem) (pii : PeriodItemInfo) = pi.ClosingStock / salesPerDay startDate endDate pii |> int

    let inline round2 x =
            Math.Round(float x, 2)



module Factory =
    open Business

    let getSalesItemInfo (s : mySalesItem) = 
        let sucu = (salesUnitsPerContainerUnit s)
        let spx = lessTax (s.TaxRate * 1.) s.SalesPrice
        let cpus = costPerUnitOfSale s.CostPerContainer s.ItemName.ContainerSize sucu
        let mu = markUp spx cpus
        let igp = grossProfit spx cpus
        { MarkUp = mu; CostPerUnitOfSale = cpus; IdealGP = igp; SalesUnitsPerContainerUnit = sucu; SalesPriceEx = spx }

    let getPeriodItemInfo ((p : myPeriodItem),(i : seq<myItemReceived>),(sii : SalesItemInfo)) = 
        let si = p.SalesItem
        let lessTax = Business.lessTax si.TaxRate
        let valueOfQuantity = valueOfQuantityT si.SalesUnitType sii.SalesUnitsPerContainerUnit si.ItemName.ContainerSize 
        let cr = i |> Seq.sumBy (fun i -> i.Quantity)
        let tu = totalUnits p.SalesItem cr p.SalesItem.ItemName.ContainerSize
        let st = round2 (p.OpeningStock + tu - p.ClosingStock)
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
        let cvsex = lessTax cvsin

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

    let piInfo i = getPeriodItemInfo ((i), i.ItemsReceived, (getSalesItemInfo i.SalesItem))

    let getPeriodInfo (p : myPeriod) = 
        let s = p.Items |> Seq.sumBy(fun i -> (piInfo i).SalesEx)
        let cvsi = p.Items |> Seq.sumBy(fun i -> (piInfo i).ClosingValueSalesInc)
        let cvse = p.Items |> Seq.sumBy(fun i -> (piInfo i).ClosingValueSalesEx)
        let cvce = p.Items |> Seq.sumBy(fun i -> (piInfo i).ClosingValueCostEx)
        {
            SalesEx = s;
            ClosingValueSalesInc = cvsi;
            ClosingValueSalesEx = cvse;
            ClosingValueCostEx = cvce;
        }

    let defaultMySalesItem = 
        {
            StockCheck.Model.mySalesItem.Id = String.Empty;
            StockCheck.Model.mySalesItem.ItemName = { LedgerCode = String.Empty; Name = String.Empty; ContainerSize = 0. }
            StockCheck.Model.mySalesItem.CostPerContainer = 0M<StockCheck.Model.money>;
            StockCheck.Model.mySalesItem.SalesPrice = 0M<StockCheck.Model.money>;
            StockCheck.Model.mySalesItem.TaxRate = 0.<StockCheck.Model.percentage>;
            StockCheck.Model.mySalesItem.UllagePerContainer = 0<StockCheck.Model.pt>;
            StockCheck.Model.mySalesItem.SalesUnitType = StockCheck.Model.salesUnitType.Other;
            StockCheck.Model.mySalesItem.OtherSalesUnit = 0.;
        }

    let defaultPeriodItem =
        {
            StockCheck.Model.myPeriodItem.Id = String.Empty;
            StockCheck.Model.myPeriodItem.SalesItem = defaultMySalesItem;
            StockCheck.Model.myPeriodItem.OpeningStock = 0.;
            StockCheck.Model.myPeriodItem.ClosingStock = 0.;
            StockCheck.Model.myPeriodItem.ClosingStockExpr = String.Empty;
            StockCheck.Model.myPeriodItem.ItemsReceived = [];
        }

    let getPeriodItem si =
        { defaultPeriodItem with SalesItem = si }

    let copyForNextPeriod (i: myPeriodItem) =
        { defaultPeriodItem with SalesItem = i.SalesItem; OpeningStock = i.ClosingStock }

    let cleanDate h m s (d : DateTime) = new DateTime(d.Year, d.Month, d.Day, h, m, s)
    let cleanStartDate = cleanDate 0 0 0
    let cleanEndDate = cleanDate 23 59 59

    let defaultPeriod = {
        StockCheck.Model.myPeriod.Id = String.Empty;
        StockCheck.Model.myPeriod.StartOfPeriod = cleanStartDate DateTime.MinValue;
        StockCheck.Model.myPeriod.EndOfPeriod = cleanEndDate DateTime.MinValue;
        StockCheck.Model.myPeriod.Name = String.Empty;
        StockCheck.Model.myPeriod.Items = []
    }

    let getPeriod id name (startOfPeriod : DateTime) (endOfPeriod : DateTime) =
        let sop = cleanStartDate startOfPeriod
        let eop = cleanEndDate endOfPeriod
        {
            StockCheck.Model.myPeriod.EndOfPeriod = eop;
            StockCheck.Model.myPeriod.Name = name;
            StockCheck.Model.myPeriod.StartOfPeriod = sop;
            StockCheck.Model.myPeriod.Items = [];
            StockCheck.Model.myPeriod.Id = id;
        }


    let initialisePeriodFromClone (p : myPeriod) = 
        let pi = p.Items 
                 |> Seq.map(fun i -> copyForNextPeriod i)
        {p with Items = pi; StartOfPeriod = cleanStartDate (p.EndOfPeriod.Date.AddDays(1.)); EndOfPeriod = cleanEndDate p.StartOfPeriod }

    let initialiseWithoutZeroCarriedItems (p : myPeriod) = 
        let ip = initialisePeriodFromClone p
        let pi = p.Items
                 |> Seq.filter (fun i -> i.OpeningStock > 0. || i.ClosingStock > 0. || (piInfo i).ContainersReceived > 0.) 
                 |> Seq.map(fun i -> copyForNextPeriod i)
        {ip with Items = pi}
