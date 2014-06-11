namespace FsWeb.Controllers

open System.Web.Http
open FsWeb.Model

type ValuesController() =
    inherit ApiController()
    let repo = new StockCheck.Repository.Query(FsWeb.Global.Store)
    let persister = new StockCheck.Repository.Persister(FsWeb.Global.Store)

    let supMap (s : StockCheck.Model.Supplier) =
        { SupplierView.Id = s.Id
          Name = s.Name }

    [<Route("api/suppliers")>]
    member x.Get() =
        let suppliers = repo.GetModelSuppliers
        suppliers |> Seq.map supMap

    [<Route("api/supplier")>]
    member x.Put(supplier : SupplierView) =
        let modelSupplier = StockCheck.Model.Supplier()
        modelSupplier.Id <- supplier.Id
        modelSupplier.Name <- supplier.Name
        persister.Save modelSupplier

    [<Route("api/salesunit")>]
    member x.GetSalesUnitTypes() = [| "Pint"; "Unit"; "Spirit"; "Fortified"; "Wine"; "Other" |]