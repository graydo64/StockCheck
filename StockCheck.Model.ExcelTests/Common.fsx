

#r @"C:\Users\graeme\Documents\GitHub\StockCheck\packages\RavenDB.Client.2.5.2879\lib\net40\Raven.Client.Lightweight.dll"
#r @"C:\Users\graeme\Documents\GitHub\StockCheck\packages\RavenDB.Client.2.5.2879\lib\net40\Raven.Abstractions.dll"

module Common =
    open Raven.Client
    open Raven.Client.Document

    let store = new DocumentStore()
    store.Url <- "http://localhost:8880/"
    store.Conventions.IdentityPartsSeparator <- "-"
    store.Initialize() |> ignore

    let session = store.OpenSession("StockCheck")

