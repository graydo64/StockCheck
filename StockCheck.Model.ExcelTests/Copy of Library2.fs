module StockCheck.Model.Update

open NUnit.Framework
open FsUnit
open System.IO
open System.Linq
open System.Xml
open StockCheck.Model
open StockCheck.Repository
open Raven.Client
open Raven.Client.Document

let store = new DocumentStore()
store.Url <- "http://localhost:8880/"
store.Conventions.IdentityPartsSeparator <- "-"
store.Initialize() |> ignore

let persister = new Persister(store)
let query = new Query(store)

//
let savePeriod (p : StockCheck.Model.Period) =
    persister.Save(p)

let saveSalesItem (s : StockCheck.Model.SalesItem) =
    persister.Save(s)

let period = query.GetModelPeriods |> Seq.filter(fun p -> p.Name = "Dec/Jan 2015") |> Seq.head

let updateSalesItem (s : StockCheck.Model.SalesItem) =
    match s.LedgerCode with
    | "1000" -> s.SalesUnitType <- Pint
    | "1005" -> s.SalesUnitType <- Pint
    | "1020" -> s.SalesUnitType <- Wine
    | "1030" -> s.SalesUnitType <- Spirit
    | _ -> s.SalesUnitType <- Unit

    persister.Save(s)

let getSalesPrice c =
    match c with
    | cpc when cpc < 95.0m -> 3.15m
    | cpc when cpc >= 95.0m && cpc < 105m -> 3.25m
    | cpc when cpc >= 105m && cpc < 120m -> 3.35m
    | cpc when cpc >= 120m -> 3.55m
    | _ -> 0m

let amountPerContainer ex qty =
    if(qty > 0.) then
        ex/(decimal qty)
    else
        0m

let maxBySI = 
    let start = System.DateTime.Now.AddDays(-60.0)
    let endDate = System.DateTime.Now
    let invoices = query.GetModelInvoicesByDateRange start endDate
    let invoiceLines = invoices |> Seq.collect(fun a -> a.InvoiceLines)
    let linesBySI = invoiceLines |> Seq.map(fun b -> (b.SalesItem.Id, amountPerContainer b.InvoicedAmountEx b.Quantity))
    linesBySI |> Seq.groupBy(fun c -> fst c) |> Seq.map(fun (k, v) -> Seq.max v)

let checkCatalogueCPC (i : StockCheck.Model.SalesItem) =
    maxBySI |> Seq.filter(fun (d, e) -> d = i.Id) |> Seq.head |> snd


[<Test>]
[<Ignore>]
let ``Update SalesItem price`` () =
    use session = store.OpenSession("StockCheck")
    let p = session.Query<StockCheck.Repository.Period>().Where(fun i -> i.Name = "Dec/Jan 2015").First()
    let items = p.Items |> Seq.filter(fun i -> i.SalesItem.LedgerCode = "1005")

    let guestItems = items |> Seq.filter(fun i -> i.SalesItem.Name <> "Everards Tiger 4.2" && i.SalesItem.Name <> "Taylor's Golden Best 3.5" && i.SalesItem.Name <> "Treboom Yorkshire Sparkle 4.0")

    guestItems 
        |> Seq.iter(fun i -> i.SalesItem.SalesPrice <- getSalesPrice i.SalesItem.CostPerContainer)
        //|> Seq.iter(fun i -> System.Console.WriteLine(System.String.Format("{0}: {1}, {2}",i.SalesItem.Name, i.SalesItem.SalesPrice,(getSalesPrice i.SalesItem))))
        |> ignore

    session.Store(p)
    session.SaveChanges()

[<Test>]
[<Ignore>]
let ``Update SalesItem unit type`` () =
    let s = query.GetModelSalesItems
    s |> Seq.iter updateSalesItem

[<Test>]
[<Ignore>]
let ``Update catalogue price`` () =
    use session = store.OpenSession("StockCheck")
    let s = session.Query<StockCheck.Repository.SalesItem>().Take(1024).AsEnumerable()
    let guestItems = s
                    |> Seq.filter(fun i -> i.LedgerCode = "1005")
                    |> Seq.filter(fun i -> i.Name <> "Everards Tiger 4.2" && i.Name <> "Taylor's Golden Best 3.5" && i.Name <> "Treboom Yorkshire Sparkle 4.0")

    let updatedPrices = guestItems |> Seq.map(fun i -> 
                                                    i.SalesPrice <- getSalesPrice i.CostPerContainer
                                                    i)
        //|> Seq.iter(fun i -> System.Console.WriteLine(System.String.Format("{0}: {1}, {2}",i.Name, i.SalesPrice,(getSalesPrice i))))
    updatedPrices |> Seq.iter(fun s -> session.Store(s))
        |> ignore

    session.SaveChanges() |> ignore
   
[<Test>]
[<Ignore>]
let ``Check catalogue cost per container`` () =
    let s = query.GetModelSalesItems
    let maxBySI = maxBySI
    let t = s
            |> Seq.filter(fun i -> i.LedgerCode = "1005")
            //|> Seq.iter(fun i -> System.conol maxBySI.Where(fun (a, b) -> a = i.Id).First())
            |> Seq.iter(fun i -> System.Console.WriteLine(System.String.Format("{0}: {1}, {2}",i.Name, i.CostPerContainer, maxBySI.Where(fun (a, b) -> a = i.Id).FirstOrDefault())))
    t |> ignore
    //|> Seq.iter(fun i -> checkCatalogueCPC i) 
    //|> ignore