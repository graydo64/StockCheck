namespace FsWeb

open System
open System.Net.Http
open System.Web
open System.Web.Http
open System.Web.Routing
open Raven.Client
open Raven.Client.Document
open System.Runtime.Caching

type HttpRoute = {
    controller : string
    id : RouteParameter }

type Global() =
    inherit System.Web.HttpApplication() 

    static member val Store = 
        (new DocumentStore(Url = "http://localhost:8880") :> IDocumentStore) with get, set

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
        Global.Store.Conventions.IdentityPartsSeparator <- "-"
        Global.Store.Initialize() |> ignore

type CacheWrapper () =
    static let cache = MemoryCache.Default
    member x.Add keyName item =
        let policy = new CacheItemPolicy()
        policy.Priority <- CacheItemPriority.Default
        policy.AbsoluteExpiration <- DateTimeOffset.Now.AddSeconds(3600.)
        cache.Set(keyName, item, policy)

    member x.Get keyName = 
        match cache.Contains(keyName) with
        | true -> Some(cache.[keyName])
        | false -> None

    member x.Remove keyName = if cache.Contains(keyName) then cache.Remove(keyName) |> ignore