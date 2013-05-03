﻿namespace StockCheck.Repository

open MongoDB.Bson
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

type Persister() =
    let collection = 
        createMongoServerWithConnString("mongodb://localhost/?connect=replicaset")
        |> getMongoDatabase "StockCheck" 
        |> getMongoCollection "StockItem"

    member this.Save (si : StockCheck.Model.SalesItem) =
        let salesItem = {
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
        salesItem
        |> collection.Insert
        |> ignore