namespace FsWeb.Controllers

open System.Web
open System.Web.Mvc
open System.Net.Http
open System.Web.Http
open StockCheck.Repository
open System
open System.ComponentModel.DataAnnotations
open System.Runtime.Serialization

[<CLIMutable>]
[<DataContract>]
type SalesItemView = 
    { [<DataMember>] Id : string
      [<DataMember>] LedgerCode : string
      [<DataMember>] Name : string
      [<DataMember>] ContainerSize : float
      [<DataMember>] CostPerContainer : decimal
      [<DataMember>] SalesPrice : decimal
      [<DataMember>] TaxRate : float
      [<DataMember>] UllagePerContainer : int
      [<DataMember>] SalesUnitsPerContainerUnit : float }

[<CLIMutable>]
[<DataContract>]
type SalesItemViewResponse = 
    { [<DataMember>] Id : string
      [<DataMember>] LedgerCode : string
      [<DataMember>] Name : string
      [<DataMember>] ContainerSize : float
      [<DataMember>] CostPerContainer : decimal
      [<DataMember>] SalesPrice : decimal
      [<DataMember>] TaxRate : float
      [<DataMember>] UllagePerContainer : int
      [<DataMember>] SalesUnitsPerContainerUnit : float
      [<DataMember>] CostPerUnitOfSale : decimal
      [<DataMember>] MarkUp : decimal
      [<DataMember>] IdealGP : float }

type SalesItemController() = 
    inherit ApiController()
    let repo = new StockCheck.Repository.Query(FsWeb.Global.Store)
    let persister = new StockCheck.Repository.Persister(FsWeb.Global.Store)
    
    member x.Get((id : string), ()) = 
        let i = repo.GetModelSalesItemById id
        { Id = i.Id
          LedgerCode = i.LedgerCode
          Name = i.Name
          ContainerSize = i.ContainerSize
          CostPerContainer = i.CostPerContainer
          SalesPrice = i.SalesPrice
          TaxRate = i.TaxRate
          UllagePerContainer = i.UllagePerContainer
          SalesUnitsPerContainerUnit = i.SalesUnitsPerContainerUnit
          CostPerUnitOfSale = i.CostPerUnitOfSale
          MarkUp = i.MarkUp
          IdealGP = i.IdealGP }
    
    member x.Put(salesItem : SalesItemView) = 
        let i = StockCheck.Model.SalesItem()
        i.Id <- salesItem.Id
        i.ContainerSize <- salesItem.ContainerSize
        i.CostPerContainer <- salesItem.CostPerContainer
        i.LedgerCode <- salesItem.LedgerCode
        i.Name <- salesItem.Name
        i.SalesPrice <- salesItem.SalesPrice
        i.SalesUnitsPerContainerUnit <- salesItem.SalesUnitsPerContainerUnit
        i.TaxRate <- salesItem.TaxRate
        i.UllagePerContainer <- salesItem.UllagePerContainer
        persister.Save(i)
