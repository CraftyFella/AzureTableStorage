module Host

open System
open Microsoft.AspNetCore.Hosting
open System.Net.Sockets
open System.Net
open Microsoft.AspNetCore.Builder
open HttpContext
open CommandHandler
open LiteDB

let private findPort () =
  TcpListener(IPAddress.Loopback, 0)
  |> fun l ->
       l.Start()
       (l, (l.LocalEndpoint :?> IPEndPoint).Port)
       |> fun (l, p) ->
            l.Stop()
            p

let private app db (appBuilder: IApplicationBuilder) =
  let inner = commandHandler db |> httpHandler
  appBuilder.Run(fun ctx -> exceptionLoggingHttpHandler inner ctx)

type FakeTables(?connectionString) =

  let connectionString =
    connectionString
    |> Option.defaultValue "filename=:memory:"

  let db =
    new LiteDatabase(connectionString, Bson.FieldValue.mapper ())

  let port = findPort ()
  let url = sprintf "http://127.0.0.1:%i" port

  let mutable connectionString =
    sprintf
      "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;TableEndpoint=http://localhost:%i/devstoreaccount1;"
      port

  let webHost =
    WebHostBuilder().Configure(fun appBuilder -> app db appBuilder).UseUrls(url)
      .UseKestrel(fun options -> options.AllowSynchronousIO <- true).Build()

  do
    if Environment.GetEnvironmentVariable("FAKEAZURETABLES_CONNECTIONSTRING")
       <> null then
      connectionString <- Environment.GetEnvironmentVariable("FAKEAZURETABLES_CONNECTIONSTRING")
    else
      webHost.Start()

  member __.ConnectionString = connectionString

  interface IDisposable with
    member __.Dispose() =
      db.Dispose()
      webHost.Dispose()
