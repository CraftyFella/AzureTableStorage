namespace TableStorageLocal

open LiteDB
open LiteDB.Engine
open System
open Microsoft.AspNetCore.Hosting

[<AutoOpen>]
module private Host =

  open System.Net.Sockets
  open System.Net
  open Microsoft.AspNetCore.Builder
  open HttpContext
  open CommandHandler

  let findPort () =
    TcpListener(IPAddress.Loopback, 0)
    |> fun l ->
         l.Start()

         (l, (l.LocalEndpoint :?> IPEndPoint).Port)
         |> fun (l, p) ->
              l.Stop()
              p

  let app db (appBuilder: IApplicationBuilder) =
    let inner = commandHandler db |> httpHandler
    appBuilder.Run(fun ctx -> exceptionLoggingHttpHandler inner ctx)

type LocalTables(connectionString: string, port: int) =

  let db =
    new LiteDatabase(connectionString, Bson.FieldValue.mapper ())

  let url = sprintf "http://*:%i" port

  let mutable connectionString =
    let host =
      if Environment.GetEnvironmentVariable "TABLESTORAGELOCAL_USEPROXY"
         |> isNull then
        "localhost"
      else
        "localhost.charlesproxy.com"

    let endpoint =
      sprintf "http://%s:%i/devstoreaccount1" host port

    sprintf
      "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;TableEndpoint=%s;TableSecondaryEndpoint=%s"
      endpoint
      endpoint

  let webHost =
    WebHostBuilder()
      .Configure(fun appBuilder -> app db appBuilder)
      .UseUrls(url)
      .UseKestrel(fun options -> options.AllowSynchronousIO <- true)
      .Build()

  do
    // Case Sensitive
    db.Rebuild(RebuildOptions(Collation = Collation.Binary))
    |> ignore

    if Environment.GetEnvironmentVariable("TABLESTORAGELOCAL_CONNECTIONSTRING")
       <> null then
      connectionString <- Environment.GetEnvironmentVariable("TABLESTORAGELOCAL_CONNECTIONSTRING")
    else
      webHost.Start()

  new() = new LocalTables("filename=:memory:", findPort ())
  new(connectionString: string) = new LocalTables(connectionString, findPort ())
  new(port: int) = new LocalTables("filename=:memory:", port)

  member __.ConnectionString = connectionString

  member __.Run = webHost.Run

  interface IDisposable with
    member __.Dispose() =
      db.Dispose()
      webHost.Dispose()
