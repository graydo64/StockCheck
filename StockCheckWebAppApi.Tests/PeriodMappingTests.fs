namespace StockCheck.WebAppApi.Tests

open System
open NUnit.Framework
open FsUnit
open FsWeb.Controllers
open FsWeb.Model
open Ploeh.AutoFixture
open System.Collections.Generic

module utils =
    // stub a reference list of salesItems by extracting the list of salesItem ids from the stubbed view model.
    let stubListofSalesItems (i : seq<PeriodItemViewModel>) =
        i |> Seq.map(fun i -> i.SalesItemId) |> Seq.map(fun s -> new StockCheck.Model.SalesItem( Id = s))

[<TestFixture>]
// Given a PeriodViewModel
// When the view model represents a new Period
// And the view model is mapped to a Model Period
// Then the model period should have the same number of PeriodItems as the view model
// And the model period should have a blank id.
type ``Given a PeriodViewModel and a new Period`` () =
    let fixture = new Fixture()
    let vm = fixture.Create<PeriodViewModel>()
    let p = new StockCheck.Model.Period()
    let l = utils.stubListofSalesItems vm.Items  
    let m = FsWeb.Model.Mapping.Period.mapPFromViewModel l p vm
        
    [<Test>] member x.
        ``The View Model Items Should Map to Model Period Items`` () =
            Seq.length m.Items |> should equal (Seq.length vm.Items)

    [<Test>] member x.
        ``The Model Period should have a blank Id`` () =
            m.Id |> should equal (String.Empty)

[<TestFixture>]
// Given a PeriodViewModel
// When the view model represents an existing Period
// And the existing period has no PeriodItems
// And the view model is mapped to the Model Period
// Then the model period should have the same number of PeriodItems as the view model
// And the model period should have the same Id as the view model.
type ``Given a PeriodViewModel and an existing Period with no items`` () =
    let fixture = new Fixture()
    let vm = fixture.Create<PeriodViewModel>()
    let p = 
        let p1 = fixture.Create<StockCheck.Model.Period>()
        p1.Items <- new List<StockCheck.Model.PeriodItem>()
        p1.Id <- vm.Id
        p1
    let l = utils.stubListofSalesItems vm.Items  
    let m = FsWeb.Model.Mapping.Period.mapPFromViewModel l p vm

    [<Test>] member x.
        ``The View Model Items Should Map to Model Period Items`` () =
            Seq.length m.Items |> should equal (Seq.length vm.Items)

    [<Test>] member x.
        ``The Model Period should have the same Id as the View Model`` () =
            m.Id |> should equal (vm.Id)

[<TestFixture>]
// Given a PeriodViewModel
// When the view model represents an existing Period
// And the Period has some PeriodItems
// And the Period's PeriodItems are all different to the view model's period items
// And the view model is mapped to a Model Period
// Then the view model's Items should concatenated to the Period's items.
type ``Given a PeriodViewModel and an existing Period with PeriodItems that aren't in the PeriodViewModel`` () =
    let fixture = new Fixture()
    let vm = fixture.Create<PeriodViewModel>()
    let p = 
        let p1 = fixture.Create<StockCheck.Model.Period>()
        p1.Id <- vm.Id
        p1
    let l1 = utils.stubListofSalesItems vm.Items  
    let l2 = p.Items |> Seq.map(fun i -> i.SalesItem)
    let l = Seq.append l1 l2
    let count = Seq.length l
    let m = FsWeb.Model.Mapping.Period.mapPFromViewModel l p vm

    [<Test>] member x.
        ``The View Model Items and the existing PeriodItems Should Map to the resulting Model Period Items`` () =
            Seq.length m.Items |> should equal (count)

[<TestFixture>]
// Given a PeriodViewModel
// When the view model represents an existing Period
// And the Period has some PeriodItems
// And one of the Period's PeriodItems has the same SalesItem as one of the view model's period items
// And the view model is mapped to a Model Period
// Then the view model's Items should concatenated to the Period's items
// And the common PeriodItem is updated with the detail from the view model.
type ``Given a PeriodViewModel and an existing Period with PeriodItems that are also in the PeriodViewModel`` () =
    let fixture = new Fixture()
    let vm = fixture.Create<PeriodViewModel>()
    let p = 
        let p1 = fixture.Create<StockCheck.Model.Period>()
        p1.Id <- vm.Id
        // make the first item in the Period.Items sequence have the same SalesItemId as the first item in PeriodViewModel.Items
        p1.Items |> Seq.head |> (fun i -> i.SalesItem.Id <- (vm.Items |> Seq.head |> (fun i -> i.SalesItemId)))
        p1

    let l1 = utils.stubListofSalesItems vm.Items  
    let l2 = p.Items |> Seq.map(fun i -> i.SalesItem)
    let l = Seq.append l1 l2
    let count = Seq.length l
    let os = p.Items |> Seq.head |> (fun i -> i.OpeningStock)
    let cs = p.Items |> Seq.head |> (fun i -> i.ClosingStock)
    let m = FsWeb.Model.Mapping.Period.mapPFromViewModel l p vm

    [<Test>] member x.
        ``The resulting Model Period Items should be the merged set of the Period.Items and PeriodViewModel.Items`` () =
            Seq.length m.Items |> should equal (count - 1)

    [<Test>] member x.
        ``The common period item should be updated with the OpeningStock figure from the view model`` () =
            m.Items |> Seq.head |> (fun i -> i.OpeningStock) |> should equal (vm.Items |> Seq.head |> (fun i -> i.OpeningStock))

    [<Test>] member x.
        ``The common period item should be updated with the ClosingStock figure from the view model`` () =
            m.Items |> Seq.head |> (fun i -> i.ClosingStock) |> should equal (vm.Items |> Seq.head |> (fun i -> i.ClosingStock))

    [<Test>] member x.
        ``The common period item should be updated with the ClosingStockExpr from the view model`` () =
            m.Items |> Seq.head |> (fun i -> i.ClosingStockExpr) |> should equal (vm.Items |> Seq.head |> (fun i -> i.ClosingStockExpr))
