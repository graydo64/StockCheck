namespace StockCheck.Model

open System
open System.Collections.Generic

type ItemName = { LedgerCode: string; Name: string; ContainerSize: float}

type SalesItem = { 
    Id: string; 
    ItemName: ItemName;
    CostPerContainer: decimal<money>;
    SalesPrice: decimal<money>;
    TaxRate: float<percentage>;
    SalesUnitType: salesUnitType;
    OtherSalesUnit: float;
    UllagePerContainer: int<pt>
}

type SalesItemInfo = {
    MarkUp : decimal<money>;
    CostPerUnitOfSale: decimal<money>;
    IdealGP: float<percentage>;
    SalesUnitsPerContainerUnit: float;
    SalesPriceEx: decimal<money>
}

type ItemReceived = {
    Id : string;
    Quantity : float;
    ReceivedDate : DateTime;
    InvoicedAmountEx : decimal<money>;
    InvoicedAmountInc : decimal<money>
}

type PeriodItem = {
    Id : string;
    OpeningStock : float;
    ClosingStockExpr : string;
    ClosingStock : float;
    SalesItem : SalesItem;
    ItemsReceived : seq<ItemReceived>;
}

type PeriodItemInfo = {
    ContainersReceived : float;
    TotalUnits : float;
    Sales : float;
    ContainersSold : float;
    PurchasesEx : decimal<money>;
    PurchasesInc : decimal<money>;
    PurchasesTotal : decimal<money>;
    SalesInc : decimal<money>;
    SalesEx : decimal<money>;
    CostOfSalesEx : decimal<money>;
    MarkUp : decimal<money>;
    ClosingValueCostEx : decimal<money>;
    ClosingValueSalesInc : decimal<money>;
    ClosingValueSalesEx : decimal<money>
}

type Period = {
    Id : string;
    Name : string;
    EndOfPeriod : DateTime;
    StartOfPeriod : DateTime;
    Items : seq<PeriodItem>;
}

type PeriodInfo = {
    SalesEx : decimal<money>;
    ClosingValueSalesInc : decimal<money>;
    ClosingValueSalesEx : decimal<money>;
    ClosingValueCostEx : decimal<money>;
}

type InvoiceLine = {
    Id : string;
    SalesItem : SalesItem;
    Quantity : float;
    InvoicedAmountEx : decimal<money>;
    InvoicedAmountInc : decimal<money>;
}

type Invoice = {
    Id : string;
    Supplier: string;
    InvoiceNumber : string;
    InvoiceDate : DateTime;
    DeliveryDate : DateTime;
    InvoiceLines : seq<InvoiceLine>
}

type Supplier = {
    Id : string;
    SupplierName : string;
}


