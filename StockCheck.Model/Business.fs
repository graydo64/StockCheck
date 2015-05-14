namespace StockCheck.Model

open System
open StockCheck.Model.Conv

module Business =

    let fromMoney (x : decimal<money>) = StockCheck.Model.money.FromMoney x
    let toMoney (x : decimal) = StockCheck.Model.money.ToMoney x

    let markUp sale cost = sale - cost

    let grossProfit (sale : decimal<money>) (cost : decimal<money>) =
        match sale with
        | 0M<money> -> 0.<percentage>
        | _ -> 
            let markUpFloat = float (fromMoney(markUp sale cost))
            let saleFloat = float (fromMoney sale)
            percentage (markUpFloat / saleFloat)

    let lessTax (rate : float<percentage>) (price : decimal<money>) =
        let denom = decimal ((1.<percentage> + rate) / 1.<percentage>)
        money (price / money denom)

    let costPerUnitOfSale (costPerContainer : decimal<money>) (containerSize : float) (salesUnitsPerContainerUnit : float) =
        let salesUnits = containerSize * salesUnitsPerContainerUnit
        let salesUnitsAsDecimal = decimal salesUnits
        let costAsDecimal = StockCheck.Model.money.FromMoney costPerContainer
        match costAsDecimal with
        | 0.M -> money 0.M
        | _ -> money costAsDecimal/salesUnitsAsDecimal

    let salesUnitsPerContainerUnit (s : SalesItem) =
        match s.SalesUnitType with
        | Pint -> float ptPerGal
        | Unit -> 1.0
        | Spirit -> float shotsPerLtr
        | Fortified -> float doublesPerLtr
        | Wine -> float wineGlassPerLtr
        | Other -> s.OtherSalesUnit

    let totalUnits (si : SalesItem) (cr : float) (cs : float) =
        match si.SalesUnitType with
        | Pint | Unit | Other -> cr * cs
        | Spirit | Fortified | Wine -> cr

    let private valueOfQuantity qty unit size (ppUnit : decimal<money>)=
        money (decimal (qty * unit * size * float (fromMoney ppUnit)))

    let valueOfQuantityT t unit size qty ppUnit =
        match t with
        | Pint | Unit -> valueOfQuantity qty unit 1.0 ppUnit
        | Spirit | Fortified | Wine | Other -> valueOfQuantity qty unit size ppUnit

    let salesPerDay (startDate: DateTime) (endDate: DateTime) (pii: PeriodItemInfo) = pii.Sales / float (endDate.Subtract(startDate).Days + 1)

    let daysOnHand (startDate: DateTime)  (endDate: DateTime) (pi: PeriodItem) (pii : PeriodItemInfo) = pi.ClosingStock / salesPerDay startDate endDate pii |> int

    let inline round2 x =
            Math.Round(float x, 2)

    let getDateOnly (d : DateTime) =
        match d.Kind with
        | DateTimeKind.Unspecified -> d.Date
        | DateTimeKind.Utc -> 
            let d1 = d.ToLocalTime().Date
            let d2 = DateTime.SpecifyKind(d1, DateTimeKind.Unspecified)
            d2
        | DateTimeKind.Local ->
            let d1 = d.Date
            let d2 = DateTime.SpecifyKind(d1, DateTimeKind.Unspecified)
            d2

module Factory =
    open Business

    let getSalesItemInfo (s : SalesItem) = 
        let sucu = (salesUnitsPerContainerUnit s)
        let spx = lessTax (s.TaxRate * 1.) s.SalesPrice
        let cpus = costPerUnitOfSale s.CostPerContainer s.ItemName.ContainerSize sucu
        let mu = markUp spx cpus
        let igp = grossProfit spx cpus
        { MarkUp = mu; CostPerUnitOfSale = cpus; IdealGP = igp; SalesUnitsPerContainerUnit = sucu; SalesPriceEx = spx }

    let getPeriodItemInfo (p : PeriodItem) = 
        let i = p.ItemsReceived
        let sii = getSalesItemInfo p.SalesItem
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
                money (decimal ((float (fromMoney pt) / tu) * st))
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

    let piInfo i = getPeriodItemInfo i

    let getPeriodInfo (p : Period) = 
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

    let (defaultSalesItem : StockCheck.Model.SalesItem) = 
        {
            Id = String.Empty;
            ItemName = { LedgerCode = String.Empty; Name = String.Empty; ContainerSize = 0. };
            CostPerContainer = 0M<StockCheck.Model.money>;
            SalesPrice = 0M<StockCheck.Model.money>;
            TaxRate = 0.<StockCheck.Model.percentage>;
            UllagePerContainer = 0<pt>;
            SalesUnitType = StockCheck.Model.salesUnitType.Other;
            OtherSalesUnit = 0.;
            ProductCode = String.Empty;
            IsActive = true;
        }

    let defaultPeriodItem =
        {
            StockCheck.Model.PeriodItem.Id = String.Empty;
            StockCheck.Model.PeriodItem.SalesItem = defaultSalesItem;
            StockCheck.Model.PeriodItem.OpeningStock = 0.;
            StockCheck.Model.PeriodItem.ClosingStock = 0.;
            StockCheck.Model.PeriodItem.ClosingStockExpr = String.Empty;
            StockCheck.Model.PeriodItem.ItemsReceived = [];
        }

    let getPeriodItem si =
        { defaultPeriodItem with SalesItem = si }

    let copyForNextPeriod (i: PeriodItem) =
        { defaultPeriodItem with SalesItem = i.SalesItem; OpeningStock = i.ClosingStock }

    let defaultPeriod = {
        StockCheck.Model.Period.Id = String.Empty;
        StockCheck.Model.Period.StartOfPeriod = DateTime.MinValue.Date;
        StockCheck.Model.Period.EndOfPeriod = DateTime.MinValue.Date;
        StockCheck.Model.Period.Name = String.Empty;
        StockCheck.Model.Period.Items = []
    }

    let getPeriod id name (startOfPeriod : DateTime) (endOfPeriod : DateTime) =
        let sop = startOfPeriod.Date
        let eop = endOfPeriod.Date
        {
            StockCheck.Model.Period.EndOfPeriod = eop;
            StockCheck.Model.Period.Name = name;
            StockCheck.Model.Period.StartOfPeriod = sop;
            StockCheck.Model.Period.Items = [];
            StockCheck.Model.Period.Id = id;
        }


    let initialisePeriodFromClone (p : Period) = 
        let pi = p.Items 
                 |> Seq.map(fun i -> copyForNextPeriod i)
        {p with Items = pi; StartOfPeriod = p.EndOfPeriod.Date.AddDays(1.); EndOfPeriod = p.EndOfPeriod.Date.AddDays(1.) }

    let initialiseWithoutZeroCarriedItems (p : Period) = 
        let ip = initialisePeriodFromClone p
        let pi = p.Items
                 |> Seq.filter (fun i -> i.OpeningStock > 0. || i.ClosingStock > 0. || (piInfo i).ContainersReceived > 0.) 
                 |> Seq.map(fun i -> copyForNextPeriod i)
        {ip with Items = pi}
