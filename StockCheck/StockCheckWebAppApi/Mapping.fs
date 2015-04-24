namespace FsWeb.Model

open FsWeb
open System
open System.Linq
open StockCheck.Repository
open StockCheck.Model.Factory

module Mapping =
    // return everything from where that isn't in except
    let notIn ( except : seq<StockCheck.Model.PeriodItem>) ( where : seq<StockCheck.Model.PeriodItem>) =
        where
        |> Seq.filter (fun i -> except |> Seq.exists  ( fun t -> t.SalesItem.Id = i.SalesItem.Id) |> not)

    module Period =

        let mapPIFromViewModel getModelSalesItemById (pi : PeriodItemViewModel) =  
            {
                StockCheck.Model.PeriodItem.Id = pi.Id;
                StockCheck.Model.PeriodItem.OpeningStock = pi.OpeningStock;
                StockCheck.Model.PeriodItem.ClosingStock = pi.ClosingStock;
                StockCheck.Model.PeriodItem.ClosingStockExpr = pi.ClosingStockExpr;
                StockCheck.Model.PeriodItem.SalesItem = getModelSalesItemById pi.SalesItemId;
                StockCheck.Model.PeriodItem.ItemsReceived = [];
            }

        let mapPFromViewModel getModelSalesItemById (p : StockCheck.Model.Period) (vm : PeriodViewModel) =
            let vmItems = vm.Items
                        |> Seq.map (fun i -> mapPIFromViewModel getModelSalesItemById i)
            let pItems = p.Items |> notIn vmItems
            let piSeq = Seq.append pItems vmItems            
            {
                StockCheck.Model.Period.EndOfPeriod = vm.EndOfPeriod;
                StockCheck.Model.Period.Name = vm.Name;
                StockCheck.Model.Period.StartOfPeriod = vm.StartOfPeriod;
                StockCheck.Model.Period.Items = piSeq;
                StockCheck.Model.Period.Id = vm.Id;
            }

        let newPFromViewModel getModelSalesItemById (vm : PeriodViewModel) =
            let piSeq = vm.Items
                        |> Seq.map (fun i -> mapPIFromViewModel getModelSalesItemById i)
            let newp = StockCheck.Model.Factory.getPeriod String.Empty vm.Name vm.StartOfPeriod vm.EndOfPeriod
            { newp with Items = piSeq }

        let mapToPI (pi : StockCheck.Model.PeriodItem) = 
            let pii = getPeriodItemInfo pi
            { PeriodItemViewModel.Id = pi.Id
              OpeningStock = pi.OpeningStock
              ClosingStockExpr = pi.ClosingStockExpr
              ClosingStock = pi.ClosingStock
              SalesItemId = pi.SalesItem.Id
              SalesItemName = pi.SalesItem.ItemName.Name
              SalesItemLedgerCode = pi.SalesItem.ItemName.LedgerCode
              ItemsReceived = pii.ContainersReceived
              SalesQty = pii.Sales
              Container = pi.SalesItem.ItemName.ContainerSize }
    
        let mapToViewModel (p : StockCheck.Model.Period) = 
            let periodInfo = StockCheck.Model.Factory.getPeriodInfo p
            { 
                PeriodViewModel.Id = p.Id
                Name = p.Name
                StartOfPeriod = p.StartOfPeriod
                EndOfPeriod = p.EndOfPeriod
                Items = p.Items |> Seq.map mapToPI
                ClosingValueCostEx = periodInfo.ClosingValueCostEx / 1M<StockCheck.Model.money>
                ClosingValueSalesInc = periodInfo.ClosingValueSalesInc / 1M<StockCheck.Model.money>
                ClosingValueSalesEx = periodInfo.ClosingValueSalesEx  / 1M<StockCheck.Model.money>
            }

