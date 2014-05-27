namespace FsWeb

open System
open System.Net.Http
open System.Web
open System.Web.Http
open System.Web.Routing
open Raven.Client
open Raven.Client.Document
open Raven.Client.Embedded

type HttpRoute = {
    controller : string
    id : RouteParameter }

type Global() =
    inherit System.Web.HttpApplication() 

    static member val Store = 
        (new DocumentStore()) with get, set

    static member RegisterWebApi(config: HttpConfiguration) =
        // Configure routing
        config.MapHttpAttributeRoutes()
        config.Routes.MapHttpRoute(
            "DefaultApi", // Route name
            "api/{controller}/{id}", // URL with parameters
            { controller = "{controller}"; id = RouteParameter.Optional } // Parameter defaults
        ) |> ignore
        // Additional Web API settings
        config.Formatters.JsonFormatter.SerializerSettings.ContractResolver <- Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()

    member x.Application_Start() =
        GlobalConfiguration.Configure(Action<_> Global.RegisterWebApi)
//        Global.Store.DataDirectory <- "App_Data\Database"
//        Global.Store.UseEmbeddedHttpServer <- true
        Global.Store.Url <- "http://localhost:8880"
        Global.Store.Conventions.IdentityPartsSeparator <- "-"
        Global.Store.Initialize() |> ignore
