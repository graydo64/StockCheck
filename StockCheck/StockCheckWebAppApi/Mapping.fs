namespace FsWeb.Model

open FsWeb
open System
open System.Linq
open StockCheck.Repository
open StockCheck.Model.Factory

module Mapping =
    module Period =

        // todo: fix this because Seq.append doesn't assign to anything.
        // side-effect - returns a PeriodItem and attempts to add it to the piitems collection
        // should just return a PeriodItem
        let mapPIFromViewModel (getModelSalesItemById : string -> StockCheck.Model.SalesItem) (pitems : seq<StockCheck.Model.PeriodItem>) (pi : PeriodItemViewModel) =  
            let periodItem = match pitems.Where(fun a -> a.SalesItem.Id = pi.SalesItemId).Any() with
                                | true -> pitems.Where(fun a -> a.SalesItem.Id = pi.SalesItemId).First()
                                | false -> 
                                        let si = getModelSalesItemById pi.SalesItemId
                                        { defaultPeriodItem with SalesItem = si }
        
            { periodItem with ClosingStock = pi.ClosingStock; OpeningStock = pi.OpeningStock; ClosingStockExpr = pi.ClosingStockExpr }                                   

        let mapPFromViewModel getModelSalesItemById (p : StockCheck.Model.Period) (vm : PeriodViewModel) =
            let piSeq = vm.Items
                        |> Seq.map (fun i -> mapPIFromViewModel getModelSalesItemById p.Items i)
            {
                StockCheck.Model.Period.EndOfPeriod = vm.EndOfPeriod;
                StockCheck.Model.Period.Name = vm.Name;
                StockCheck.Model.Period.StartOfPeriod = vm.StartOfPeriod;
                StockCheck.Model.Period.Items = piSeq;
                StockCheck.Model.Period.Id = vm.Id;
            }

        let newPFromViewModel getModelSalesItemById (vm : PeriodViewModel) =
            let piSeq = vm.Items
                        |> Seq.map (fun i -> mapPIFromViewModel getModelSalesItemById [] i)
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

