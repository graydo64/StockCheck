namespace StockCheck.Model

open System
open System.Collections.Generic

type ItemName = { LedgerCode: string; Name: string; ContainerSize: float}

type mySalesItem = { 
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

type myItemReceived = {
    Id : string;
    Quantity : float;
    ReceivedDate : DateTime;
    InvoicedAmountEx : decimal<money>;
    InvoicedAmountInc : decimal<money>
}

type myPeriodItem = {
    Id : string;
    OpeningStock : float;
    ClosingStockExpr : string;
    ClosingStock : float;
    SalesItem : mySalesItem;
    ItemsReceived : List<myItemReceived>;
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

type myPeriod = {
    Id : string;
    Name : string;
    EndOfPeriod : DateTime;
    StartOfPeriod : DateTime;
    Items : List<myPeriodItem * PeriodItemInfo>;
}

type PeriodInfo = {
    SalesEx : decimal<money>;
    ClosingValueSalesInc : decimal<money>;
    ClosingValueSalesEx : decimal<money>;
    ClosingValueCostEx : decimal<money>;
}

type myInvoiceLine = {
    Id : string;
    SalesItem : mySalesItem;
    Quantity : float;
    InvoicedAmountEx : decimal<money>;
    InvoicedAmountInc : decimal<money>;
}

type myInvoice = {
    Id : string;
    Supplier: string;
    InvoiceNumber : string;
    InvoiceDate : DateTime;
    DeliveryDate : DateTime;
    InvoiceLines : List<InvoiceLine>
}

type mySupplier = {
    Id : string;
    Name : string;
}


