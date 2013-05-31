namespace StockCheck.Repository

open System
open System.Linq
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
    ItemsReceived : ItemReceived seq
}

[<CLIMutable>]
type Period = {
    _id : ObjectId
    Name : string
    StartOfPeriod : DateTime
    EndOfPeriod : DateTime
    Items : PeriodItem seq
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
            Id = si._id.ToString(),
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
        let items = p.Items |> Seq.map piMap
        StockCheck.Model.Period ( 
            Id = p._id.ToString(),
            Name = p.Name, 
            StartOfPeriod = p.StartOfPeriod, 
            EndOfPeriod = p.EndOfPeriod, 
            Items = System.Collections.Generic.List<StockCheck.Model.PeriodItem> items
            )

type Query(connectionString : string) =
    let db = createMongoServerWithConnString(connectionString)
             |> getMongoDatabase "StockCheck"

    let getModelSalesItem (si : SalesItem) =
        MapToModel.siMap si
    
    member internal this.GetSalesItem (name : string) (ledgerCode : string) =
        let collection = db |> getMongoCollection<SalesItem> "SalesItem"
        let value = BsonValue.Create(name)
        collection.FindOne(Query.EQ("Name", value))

    member internal this.GetSalesItemById (id : string) =
        let collection = db |> getMongoCollection<SalesItem> "SalesItem"
        let value = BsonValue.Create(ObjectId.Parse(id))
        collection.FindOne(Query.EQ("_id", value))        

    member internal this.GetPeriod (name : string) =
        let collection = db |> getMongoCollection<Period> "Period"
        let value = BsonValue.Create(name)
        collection.FindOne(Query.EQ("Name", value))

    member internal this.GetPeriods =
        let collection = db |> getMongoCollection<Period> "Period"
        collection.FindAll().ToList<Period>()

    member internal this.GetSalesItems =
        let collection = db |> getMongoCollection<SalesItem> "SalesItem"
        collection.FindAll().ToList<SalesItem>()

    member this.GetModelSalesItemById (id : string) =
        this.GetSalesItemById id |> MapToModel.siMap

    member this.GetModelSalesItem (name : string) (ledgerCode : string) =
        this.GetSalesItem name ledgerCode |> MapToModel.siMap 

    member this.GetModelPeriod period =
        MapToModel.pMap period

    member this.GetModelPeriods =
        this.GetPeriods |> Seq.map this.GetModelPeriod

    member this.GetModelSalesItems =
        this.GetSalesItems |> Seq.map getModelSalesItem
        

module internal MapFromModel = 
    let irMap (ir : StockCheck.Model.ItemReceived) =
        { Quantity = ir.Quantity;
            ReceivedDate = ir.ReceivedDate;
            InvoicedAmountEx = float ir.InvoicedAmountEx;
            InvoicedAmountInc = float ir.InvoicedAmountInc
        }

    let piMap (connectionString : string) (pi : StockCheck.Model.PeriodItem) =
        let query = new Query(connectionString)
        { SalesItem = query.GetSalesItem pi.SalesItem.Name  pi.SalesItem.LedgerCode;
            OpeningStock = pi.OpeningStock;
            ClosingStock = pi.ClosingStock;
            ItemsReceived = pi.ItemsReceived |> Seq.map (fun i -> irMap i) |> List.ofSeq
        }

    let siMap (si : StockCheck.Model.SalesItem) =
        {
            _id = match si.Id with
                    | sid when sid = String.Empty -> ObjectId()
                    | _ -> ObjectId.Parse(si.Id);
            ContainerSize = si.ContainerSize;
            CostPerContainer = si.CostPerContainer;
            LedgerCode = si.LedgerCode;
            Name = si.Name;
            SalesPrice = si.SalesPrice;
            TaxRate = si.TaxRate;
            UllagePerContainer = si.UllagePerContainer;
            SalesUnitsPerContainerUnit = si.SalesUnitsPerContainerUnit
        }

    let pMap (connectionString : string) (p : StockCheck.Model.Period) =
        {
            _id = match p.Id with
                    | pid when pid = String.Empty -> ObjectId()
                    | _ -> ObjectId.Parse(p.Id);
            Name = p.Name;
            StartOfPeriod = p.StartOfPeriod;
            EndOfPeriod = p.EndOfPeriod;
            Items = p.Items |> Seq.map (fun i -> piMap connectionString i) |> List.ofSeq
        }

type Persister(connectionString : string) =

    let db = createMongoServerWithConnString(connectionString)
             |> getMongoDatabase "StockCheck"

    let periodMap = MapFromModel.pMap connectionString

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
        periodMap p
        |> collection.Save
        |> ignore