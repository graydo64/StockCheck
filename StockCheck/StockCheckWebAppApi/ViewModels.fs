namespace FsWeb.Model

open System
open System.Collections.Generic
open System.Runtime.Serialization

[<DataContract>]
type ItemReceivedViewModel =
    { [<DataMember>] Quantity : float
      [<DataMember>] ReceivedDate : DateTime
      [<DataMember>] InvoicedAmountEx : decimal
      [<DataMember>] InvoicedAmountInc : decimal
      [<DataMember>] Reference : string }

[<DataContract>]
type InvoiceLineViewModel =
    { [<DataMember>] Id : string
      [<DataMember>] SalesItemId : string
      [<DataMember>] SalesItemDescription : string
      [<DataMember>] Quantity : float
      [<DataMember>] InvoicedAmountEx : decimal
      [<DataMember>] InvoicedAmountInc : decimal }

[<DataContract>]
type InvoiceViewModel =
    { [<DataMember>] Id : string
      [<DataMember>] Supplier : string
      [<DataMember>] InvoiceNumber : string
      [<DataMember>] InvoiceDate : DateTime
      [<DataMember>] DeliveryDate : DateTime
      [<DataMember>] InvoiceLines : List<InvoiceLineViewModel>
      [<DataMember>] TotalEx : decimal
      [<DataMember>] TotalInc : decimal }

[<DataContract>]
type PeriodItemViewModel =
    { [<DataMember>] Id : string
      [<DataMember>] OpeningStock : float
      [<DataMember>] ClosingStockExpr : string
      [<DataMember>] ClosingStock : float
      [<DataMember>] SalesItemId : string
      [<DataMember>] SalesItemLedgerCode : string
      [<DataMember>] SalesItemName : string
      [<DataMember>] Container : float
      [<DataMember>] ItemsReceived : float
      [<DataMember>] SalesQty : float }

[<DataContract>]
type PeriodsViewModel =
    { [<DataMember>] Id : string
      [<DataMember>] Name : string
      [<DataMember>] StartOfPeriod : DateTime
      [<DataMember>] EndOfPeriod : DateTime
      [<DataMember>] SalesEx : decimal
      [<DataMember>] ClosingValueCostEx : decimal }

[<DataContract>]
type PeriodViewModel =
    { [<DataMember>] Id : string
      [<DataMember>] Name : string
      [<DataMember>] StartOfPeriod : DateTime
      [<DataMember>] EndOfPeriod : DateTime
      [<DataMember>] Items : seq<PeriodItemViewModel>
      [<DataMember>] ClosingValueCostEx : decimal
      [<DataMember>] ClosingValueSalesInc : decimal
      [<DataMember>] ClosingValueSalesEx : decimal }

[<DataContract>]
type SalesItemsViewModel =
    { [<DataMember>] Id : string
      [<DataMember>] LedgerCode : string
      [<DataMember>] Name : string
      [<DataMember>] ContainerSize : float
      [<DataMember>] CostPerContainer : decimal
      [<DataMember>] SalesPrice : decimal
      [<DataMember>] SalesUnitType : string
      [<DataMember>] ProductCode : string }

[<DataContract>]
type SalesItemView =
    { [<DataMember>] Id : string
      [<DataMember>] LedgerCode : string
      [<DataMember>] Name : string
      [<DataMember>] ContainerSize : float
      [<DataMember>] CostPerContainer : decimal
      [<DataMember>] SalesPrice : decimal
      [<DataMember>] TaxRate : float
      [<DataMember>] UllagePerContainer : int
      [<DataMember>] SalesUnitType : string
      [<DataMember>] SalesUnitsPerContainerUnit : float
      [<DataMember>] ProductCode : string
      [<DataMember>] IsActive : bool }

[<DataContract>]
type SalesItemViewResponse =
    { [<DataMember>] Id : string
      [<DataMember>] LedgerCode : string
      [<DataMember>] Name : string
      [<DataMember>] ContainerSize : float
      [<DataMember>] CostPerContainer : decimal
      [<DataMember>] SalesPrice : decimal
      [<DataMember>] TaxRate : float
      [<DataMember>] UllagePerContainer : int
      [<DataMember>] SalesUnitType : string
      [<DataMember>] SalesUnitsPerContainerUnit : float
      [<DataMember>] CostPerUnitOfSale : decimal
      [<DataMember>] MarkUp : decimal
      [<DataMember>] IdealGP : float
      [<DataMember>] ProductCode : string
      [<DataMember>] IsActive : bool }

[<DataContract>]
type SupplierView =
    { [<DataMember>] Id : string
      [<DataMember>] Name : string }