namespace StockCheck.Model.Test

open NUnit.Framework
open FsUnit
open StockCheck.Model
open StockCheck.Model.Conv
open StockCheck.Model.Factory
open Ploeh.AutoFixture

[<TestFixture>]
type ``Given that the cost per container is zero`` () =
    let salesItem = { Factory.defaultMySalesItem with CostPerContainer = 0M<money> }
    let salesItemInfo = getSalesItemInfo salesItem
    [<Test>] member x.
        ``The cost per unit of sale is zero`` () =
            salesItemInfo.CostPerUnitOfSale |> should equal 0

[<TestFixture>]
type ``Given that the item is draught and the cost of sale is greater than zero`` () =
    let containerSize = 11.
    let costPerContainer = 110m<money>
    let dsi = defaultMySalesItem
    let salesItem = { dsi with ItemName = { dsi.ItemName with ContainerSize = containerSize }; SalesUnitType = Pint; CostPerContainer = costPerContainer }
    let salesItemInfo = getSalesItemInfo salesItem

    [<Test>] member x.
        ``The cost per unit of sale should be calculated correctly`` () =
            salesItemInfo.CostPerUnitOfSale |> should equal (costPerContainer / (decimal containerSize * 8.M))

    [<Test>] member x.
        ``The number of sales units per container unit should be calculated`` () =
            salesItemInfo.SalesUnitsPerContainerUnit |> should equal (8.)

[<TestFixture>]
type ``Given that the item is bottled and the cost of sale is greater than zero`` () =
    let containerSize = 24.
    let costPerContainer = 11.50m<money>
    let salesUnitType = Unit
    let salesPrice = 3.52m<money>
    let taxRate = 0.2<percentage>
    let dsi = defaultMySalesItem
    let salesItem = { dsi with ItemName = { dsi.ItemName with ContainerSize = containerSize }; SalesUnitType = Unit; CostPerContainer = costPerContainer; SalesPrice = salesPrice; TaxRate = taxRate }
    let salesItemInfo = getSalesItemInfo salesItem

    let costPerUnitOfSale (costPerContainer : decimal<money>) containerSize (saleUnitsPerContainerUnit : float) =
        costPerContainer / decimal (containerSize * saleUnitsPerContainerUnit)

    [<Test>] member x.
        ``The cost per unit of sale should be calculated correctly`` () =
            salesItemInfo.CostPerUnitOfSale |> should equal (costPerUnitOfSale costPerContainer containerSize 1.0)

    [<Test>] member x.
        ``The number of sales units per container unit should be calculated`` () =
            salesItemInfo.SalesUnitsPerContainerUnit |> should equal (1.0)

    [<Test>] member x.
        ``The markup should be calculated correctly`` () =
            salesItemInfo.MarkUp |> should equal (salesPrice / decimal (1.0 + taxRate/1.0<percentage>) - costPerUnitOfSale costPerContainer containerSize 1.0)

[<TestFixture>]
type ``Given that the item is spirit and the cost of sale is greater than zero`` () =
    let containerSize = 0.7
    let unitOfSale = 0.035
    let costPerContainer = 22m<money>
    let dsi = defaultMySalesItem
    let salesItem = { dsi with ItemName = { dsi.ItemName with ContainerSize = containerSize }; SalesUnitType = Spirit; CostPerContainer = costPerContainer }
    let salesItemInfo = getSalesItemInfo salesItem

    [<Test>] member x.
        ``The cost per unit of sale should be calculated correctly`` () =
            salesItemInfo.CostPerUnitOfSale |> should equal (costPerContainer / decimal (containerSize / unitOfSale))

    [<Test>] member x.
        ``The number of sales units per container unit should be calculated`` () =
            salesItemInfo.SalesUnitsPerContainerUnit |> should equal (1.0 / unitOfSale)


[<TestFixture>]
type ``Given that the Sales Price is zero`` () =
    let salesItem = { defaultMySalesItem with SalesPrice = 0m<money> }
    let salesItemInfo = getSalesItemInfo salesItem

    [<Test>] member x.
        ``The GP is zero`` () =
            salesItemInfo.IdealGP |> should equal 0

[<TestFixture>]
type ``Given that the Sales Price double the Cost price`` () =
    let dsi = defaultMySalesItem
    let salesItem = { dsi with SalesPrice = 2.4m<money>; ItemName = { dsi.ItemName with ContainerSize = 1. }; SalesUnitType = Unit; CostPerContainer = 1m<money>; TaxRate = 0.2<percentage> }
    let lessTax taxRate (salesPrice : decimal<money>) = salesPrice / (decimal 1.0 + decimal taxRate)
    let salesItemInfo = getSalesItemInfo salesItem

    [<Test>] member x.
        ``The GP is 50 percent`` () =
            salesItemInfo.IdealGP |> should equal 0.5

    [<Test>] member x.
        ``The Mark-Up should be calculated correctly`` () =
            salesItemInfo.MarkUp |> should equal (lessTax salesItem.TaxRate salesItem.SalesPrice - salesItemInfo.CostPerUnitOfSale)

[<TestFixture>]
type ``Given a draught product with a Sales Price`` () =
    let dsi = defaultMySalesItem
    let salesItem = { dsi with SalesPrice = 3.60m<money>; ItemName = { dsi.ItemName with ContainerSize = 9. }; SalesUnitType = Pint; CostPerContainer = 144m<money> }
    let salesItemInfo = getSalesItemInfo salesItem

    [<Test>] member x.
        ``The Mark-Up should be 1.60`` () =
            salesItemInfo.MarkUp |> should equal 1.60m