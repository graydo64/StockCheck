namespace StockCheck.WebAppApi.Tests

open System
open NUnit.Framework
open FsUnit
open FsWeb.Controllers
open FsWeb.Model
open Ploeh.AutoFixture
open StockCheck.Model.Factory

[<TestFixture>]
type ``Given a PeriodViewModel and a new Period`` () =
    let fixture = new Fixture()
    let vm = fixture.Create<PeriodViewModel>()
    let stub (s : string) = defaultSalesItem
    let m = FsWeb.Model.Mapping.Period.newPFromViewModel stub vm

    [<Test>] member x.
        ``The View Model Items Should Map to Model Period Items`` () =
            Seq.length m.Items |> should equal (Seq.length vm.Items)

    [<Test>] member x.
        ``The Model Period should have a blank Id`` () =
            m.Id |> should equal (String.Empty)

[<TestFixture>]
type ``Given a PeriodViewModel and an existing Period`` () =
    let fixture = new Fixture()
    let vm = fixture.Create<PeriodViewModel>()
    let stub (s : string) = defaultSalesItem
    let p = fixture.Create<StockCheck.Model.Period>()
    let m = FsWeb.Model.Mapping.Period.mapPFromViewModel stub p vm

    [<Test>] member x.
        ``The View Model Items Should Map to Model Period Items`` () =
            Seq.length m.Items |> should equal (Seq.length vm.Items)

    [<Test>] member x.
        ``The Model Period should have the same Id as the View Model`` () =
            m.Id |> should equal (vm.Id)

