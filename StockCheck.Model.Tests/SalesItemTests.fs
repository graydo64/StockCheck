﻿namespace StockCheck.Model.Test

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
    let salesItem = new SalesItem(ContainerSize = containerSize, SalesUnitType = Pint, CostPerContainer = costPerContainer)

    [<Test>] member x.
        ``The cost per unit of sale should be calculated correctly`` () =
            salesItem.CostPerUnitOfSale |> should equal (float costPerContainer / (containerSize * 8.))

    [<Test>] member x.
        ``The number of sales units per container unit should be calculated`` () =
            salesItem.SalesUnitsPerContainerUnit |> should equal (8.)

[<TestFixture>]
type ``Given that the item is bottled and the cost of sale is greater than zero`` () =
    let containerSize = 24.
    let costPerContainer = 11.50m
    let salesUnitType = Unit
    let salesPrice = 3.52m
    let taxRate = 0.2
    let salesItem = new SalesItem(ContainerSize = containerSize, SalesUnitType = Unit, CostPerContainer = costPerContainer, SalesPrice = salesPrice, TaxRate = taxRate)

    let costPerUnitOfSale costPerContainer containerSize saleUnitsPerContainerUnit =
        decimal (float costPerContainer / (containerSize * saleUnitsPerContainerUnit))

    [<Test>] member x.
        ``The cost per unit of sale should be calculated correctly`` () =
            salesItem.CostPerUnitOfSale |> should equal (costPerUnitOfSale costPerContainer containerSize 1.0)

    [<Test>] member x.
        ``The number of sales units per container unit should be calculated`` () =
            salesItem.SalesUnitsPerContainerUnit |> should equal (1.0)

    [<Test>] member x.
        ``The markup should be calculated correctly`` () =
            salesItem.MarkUp |> should equal (salesPrice / (decimal 1.0 + decimal taxRate) - costPerUnitOfSale costPerContainer containerSize 1.0)

[<TestFixture>]
type ``Given that the item is spirit and the cost of sale is greater than zero`` () =
    let containerSize = 0.7
    let unitOfSale = 0.035
    let costPerContainer = 22m
    let salesItem = new SalesItem(ContainerSize = containerSize, SalesUnitType = Spirit, CostPerContainer = costPerContainer)

    [<Test>] member x.
        ``The cost per unit of sale should be calculated correctly`` () =
            salesItem.CostPerUnitOfSale |> should equal (decimal (float costPerContainer / (containerSize / unitOfSale)))

    [<Test>] member x.
        ``The number of sales units per container unit should be calculated`` () =
            salesItem.SalesUnitsPerContainerUnit |> should equal (1.0 / unitOfSale)


[<TestFixture>]
type ``Given that the Sales Price is zero`` () =
    let salesItem = new SalesItem(SalesPrice = 0m)

    [<Test>] member x.
        ``The GP is zero`` () =
            salesItem.IdealGP |> should equal 0

[<TestFixture>]
type ``Given that the Sales Price double the Cost price`` () =
    let salesItem = new SalesItem(SalesPrice = 2.4m, ContainerSize = 1., SalesUnitType = Unit, CostPerContainer = 1m, TaxRate = 0.2)
    let lessTax taxRate salesPrice = salesPrice / (decimal 1.0 + decimal taxRate)

    [<Test>] member x.
        ``The GP is 50 percent`` () =
            salesItem.IdealGP |> should equal 0.5

    [<Test>] member x.
        ``The Mark-Up should be calculated correctly`` () =
            salesItem.MarkUp |> should equal (lessTax salesItem.TaxRate salesItem.SalesPrice - salesItem.CostPerUnitOfSale)

[<TestFixture>]
type ``Given a draught product with a Sales Price`` () =
    let salesItem = new SalesItem(SalesPrice = 3.60m, ContainerSize = 9., SalesUnitType = Pint, CostPerContainer = 144m)

    [<Test>] member x.
        ``The Mark-Up should be 1.60`` () =
            salesItem.MarkUp |> should equal 1.60m