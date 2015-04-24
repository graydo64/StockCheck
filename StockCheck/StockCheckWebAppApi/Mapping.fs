namespace FsWeb.Model

open System
open System.Linq

module Mapping =
    module Period =
        let mapToPI (pi : StockCheck.Model.PeriodItem) = 
            { 
                PeriodItemViewModel.Id = pi.Id
                OpeningStock = pi.OpeningStock
                ClosingStockExpr = pi.ClosingStockExpr
                ClosingStock = pi.ClosingStock
                SalesItemId = pi.SalesItem.Id
                SalesItemName = pi.SalesItem.Name
                SalesItemLedgerCode = pi.SalesItem.LedgerCode
                ItemsReceived = pi.ContainersReceived
                SalesQty = pi.Sales
                Container = pi.SalesItem.ContainerSize 
            }
    
        let mapToViewModel (p : StockCheck.Model.Period) = 
            { PeriodViewModel.Id = p.Id
              Name = p.Name
              StartOfPeriod = p.StartOfPeriod
              EndOfPeriod = p.EndOfPeriod
              Items = p.Items |> Seq.map mapToPI
              ClosingValueCostEx = p.ClosingValueCostEx
              ClosingValueSalesInc = decimal p.ClosingValueSalesInc
              ClosingValueSalesEx = decimal p.ClosingValueSalesEx }
    
        let mapPIFromViewModel (salesItems : seq<StockCheck.Model.SalesItem>) (p : StockCheck.Model.Period) (pi : PeriodItemViewModel) =  
            let periodItem = match p.Items.Where(fun a -> a.SalesItem.Id = pi.SalesItemId).Any() with
                                | true -> p.Items.Where(fun a -> a.SalesItem.Id = pi.SalesItemId).First()
                                | false -> let ni = new StockCheck.Model.PeriodItem(
                                                                            salesItems
                                                                            |> Seq.filter(fun i -> i.Id = pi.SalesItemId)
                                                                            |> Seq.head) 
                                           p.Items.Add(ni)
                                           ni
                                            
            periodItem.ClosingStock <- pi.ClosingStock
            periodItem.OpeningStock <- pi.OpeningStock
            periodItem.ClosingStockExpr <- pi.ClosingStockExpr

        let mapPFromViewModel (salesItems : seq<StockCheck.Model.SalesItem>) (p : StockCheck.Model.Period) (vm : PeriodViewModel) =
            p.EndOfPeriod <- vm.EndOfPeriod
            p.Name <- vm.Name
            p.StartOfPeriod <- vm.StartOfPeriod
            vm.Items
            |> Seq.iter (fun i -> mapPIFromViewModel salesItems p i)
            p


