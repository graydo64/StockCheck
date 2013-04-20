namespace StockCheck.Model.Test

open NUnit.Framework
open FsUnit
open StockCheck.ModelFs
open Ploeh.AutoFixture

[<TestFixture>]
type ``Given that the cost per container is zero`` () =
    let salesItem = new SalesItem (CostPerContainer = decimal 0)

    [<Test>] member x.
        ``The cost per unit of sale is zero`` () =
            salesItem.CostPerUnitOfSale |> should equal 0

[<TestFixture>]
type ``Given that the item is draught and the cost of sale is greater than zero`` () =
    let containerSize = 11.
    let unitOfSale = 1. / 8.
    let costPerContainer = 110m
    let saleUnitsPerContainer = containerSize / unitOfSale
    let salesItem = new SalesItem(ContainerSize = containerSize, UnitOfSale = unitOfSale, CostPerContainer = costPerContainer)

    [<Test>] member x.
        ``The cost per unit of sale should be calculated correctly`` () =
            salesItem.CostPerUnitOfSale |> should equal (float costPerContainer / saleUnitsPerContainer)

