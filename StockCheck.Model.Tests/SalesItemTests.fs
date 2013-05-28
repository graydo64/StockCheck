namespace StockCheck.Model.Test

open NUnit.Framework
open FsUnit
open StockCheck.Model
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
    let costPerContainer = 110m
    let saleUnitsPerContainerUnit = 8.
    let salesItem = new SalesItem(ContainerSize = containerSize, SalesUnitsPerContainerUnit = saleUnitsPerContainerUnit, CostPerContainer = costPerContainer)

    [<Test>] member x.
        ``The cost per unit of sale should be calculated correctly`` () =
            salesItem.CostPerUnitOfSale |> should equal (float costPerContainer / (containerSize * saleUnitsPerContainerUnit))

[<TestFixture>]
type ``Given that the item is bottled and the cost of sale is greater than zero`` () =
    let containerSize = 24.
    let costPerContainer = 11.50m
    let saleUnitsPerContainerUnit = 1.
    let salesItem = new SalesItem(ContainerSize = containerSize, SalesUnitsPerContainerUnit = saleUnitsPerContainerUnit, CostPerContainer = costPerContainer)

    [<Test>] member x.
        ``The cost per unit of sale should be calculated correctly`` () =
            salesItem.CostPerUnitOfSale |> should equal (decimal (float costPerContainer / (containerSize * saleUnitsPerContainerUnit)))

[<TestFixture>]
type ``Given that the item is spirit and the cost of sale is greater than zero`` () =
    let containerSize = 0.7
    let unitOfSale = 0.035
    let costPerContainer = 22m
    let saleUnitsPerContainerUnit = 1. / unitOfSale
    let salesItem = new SalesItem(ContainerSize = containerSize, SalesUnitsPerContainerUnit = saleUnitsPerContainerUnit, CostPerContainer = costPerContainer)

    [<Test>] member x.
        ``The cost per unit of sale should be calculated correctly`` () =
            salesItem.CostPerUnitOfSale |> should equal (decimal (float costPerContainer / (containerSize * saleUnitsPerContainerUnit)))

[<TestFixture>]
type ``Given that the Sales Price is zero`` () =
    let salesItem = new SalesItem(SalesPrice = 0m)

    [<Test>] member x.
        ``The GP is zero`` () =
            salesItem.IdealGP |> should equal 0

[<TestFixture>]
type ``Given that the Sales Price double the Cost price`` () =
    let salesItem = new SalesItem(SalesPrice = 2m, ContainerSize = 1., SalesUnitsPerContainerUnit = 1., CostPerContainer = 1m)

    [<Test>] member x.
        ``The GP is 50 percent`` () =
            salesItem.IdealGP |> should equal 0.5

    [<Test>] member x.
        ``The Mark-Up should be calculated correctly`` () =
            salesItem.MarkUp |> should equal (salesItem.SalesPrice - salesItem.CostPerUnitOfSale)

[<TestFixture>]
type ``Given a draught product with a Sales Price`` () =
    let salesItem = new SalesItem(SalesPrice = 3.25m, ContainerSize = 9., SalesUnitsPerContainerUnit = 8., CostPerContainer = 144m)

    [<Test>] member x.
        ``The Mark-Up should be 1.25`` () =
            salesItem.MarkUp |> should equal 1.25m