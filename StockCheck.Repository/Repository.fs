namespace StockCheck.Repository

open System
open MongoDB.Bson
open MongoDB.Driver.Builders
open System.ComponentModel.DataAnnotations

[<CLIMutable>]
type SalesItem = {
    _id : ObjectId
    ContainerSize : float
    CostPerContainer : decimal
    LedgerCode : string
    Name : string
    SalesPrice : decimal
    TaxRate : float
    UllagePerContainer : int
    SalesUnitsPerContainerUnit : float
}

[<CLIMutable>]
type ItemReceived = {
    Quantity : float
    ReceivedDate : DateTime
    InvoicedAmountEx : float
    InvoicedAmountInc : float
}

[<CLIMutable>]
type PeriodItem = {
    SalesItem : SalesItem
    OpeningStock : float
    ClosingStock : float
    ItemsReceived : ItemReceived list
}

[<CLIMutable>]
type Period = {
    _id : ObjectId
    Name : string
    StartOfPeriod : DateTime
    EndOfPeriod : DateTime
    Items : PeriodItem list
}

module internal MapToModel =

    let irMap (ir : ItemReceived) =
        StockCheck.Model.ItemReceived ( 
            Quantity = ir.Quantity, 
            ReceivedDate = ir.ReceivedDate, 
            InvoicedAmountEx = decimal ir.InvoicedAmountEx, 
            InvoicedAmountInc = decimal ir.InvoicedAmountInc
            )

    let siMap (si : SalesItem) =
        StockCheck.Model.SalesItem ( 
            ContainerSize = si.ContainerSize,
            CostPerContainer = si.CostPerContainer,
            LedgerCode = si.LedgerCode,
            Name = si.Name,
            SalesPrice = si.SalesPrice,
            TaxRate = si.TaxRate,
            UllagePerContainer = si.UllagePerContainer,
            SalesUnitsPerContainerUnit = si.SalesUnitsPerContainerUnit
            )

    let piMap (pi : PeriodItem) = 
        let modelItem = StockCheck.Model.PeriodItem(siMap pi.SalesItem)
        modelItem.OpeningStock <- pi.OpeningStock
        modelItem.ClosingStock <- pi.ClosingStock
        pi.ItemsReceived |> Seq.map (fun i -> modelItem.ReceiveItems i.ReceivedDate i.Quantity (decimal i.InvoicedAmountEx) (decimal i.InvoicedAmountInc)) |> ignore
        modelItem

    let pMap (p : Period) =
        let items = p.Items |> List.map (fun i -> piMap i)
        StockCheck.Model.Period ( 
            Name = p.Name, 
            StartOfPeriod = p.StartOfPeriod, 
            EndOfPeriod = p.EndOfPeriod, 
            Items = System.Collections.Generic.List<StockCheck.Model.PeriodItem> items
            )

type Query() =
    let db = createMongoServerWithConnString("mongodb://localhost/?connect=replicaset")
             |> getMongoDatabase "StockCheck"
    
    member internal this.GetSalesItem (name : string) (ledgerCode : string) =
        let collection = db |> getMongoCollection<SalesItem> "SalesItem"
        let value = BsonValue.Create(name)
        collection.FindOne(Query.EQ("Name", value))

    member this.GetModelSalesItem (name : string) (ledgerCode : string) =
        this.GetSalesItem name ledgerCode |> MapToModel.siMap 

    member internal this.GetPeriod (name : string) =
        let collection = db |> getMongoCollection<Period> "Period"
        let value = BsonValue.Create(name)
        collection.FindOne(Query.EQ("Name", value))

    member this.GetModelPeriod period =
        MapToModel.pMap period

module internal MapFromModel = 
    let irMap (ir : StockCheck.Model.ItemReceived) =
        { Quantity = ir.Quantity;
            ReceivedDate = ir.ReceivedDate;
            InvoicedAmountEx = float ir.InvoicedAmountEx;
            InvoicedAmountInc = float ir.InvoicedAmountInc
        }

    let piMap (pi : StockCheck.Model.PeriodItem) =
        let query = new Query()
        { SalesItem = query.GetSalesItem pi.SalesItem.Name  pi.SalesItem.LedgerCode;
            OpeningStock = pi.OpeningStock;
            ClosingStock = pi.ClosingStock;
            ItemsReceived = pi.ItemsReceived |> Seq.map (fun i -> irMap i) |> List.ofSeq
        }

    let siMap (si : StockCheck.Model.SalesItem) =
        {
            _id = ObjectId();
            ContainerSize = si.ContainerSize;
            CostPerContainer = si.CostPerContainer;
            LedgerCode = si.LedgerCode;
            Name = si.Name;
            SalesPrice = si.SalesPrice;
            TaxRate = si.TaxRate;
            UllagePerContainer = si.UllagePerContainer;
            SalesUnitsPerContainerUnit = si.SalesUnitsPerContainerUnit
        }

    let pMap (p : StockCheck.Model.Period) =
        {
            _id = ObjectId();
            Name = p.Name;
            StartOfPeriod = p.StartOfPeriod;
            EndOfPeriod = p.EndOfPeriod;
            Items = p.Items |> Seq.map (fun i -> piMap i) |> List.ofSeq
        }

type Persister() =

    let db = createMongoServerWithConnString("mongodb://localhost/?connect=replicaset")
             |> getMongoDatabase "StockCheck"

    member this.Save (si : StockCheck.Model.SalesItem) =
        let collection = 
            db
            |> getMongoCollection<SalesItem> "SalesItem"

        MapFromModel.siMap si
        |> collection.Save
        |> ignore

    member this.Save (p : StockCheck.Model.Period) = 
        let collection = 
            db
            |> getMongoCollection<Period> "Period"
        MapFromModel.pMap p
        |> collection.Save
        |> ignore