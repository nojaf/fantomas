module Fantomas.CoreGlobalTool.Tests.DaemonTests

open System
open System.IO
open Fantomas.CoreGlobalTool.Daemon
open LspTypes
open NUnit.Framework
open Nerdbank.Streams
open Serilog
open StreamJsonRpc

[<Test>]
let ``client can connect to daemon`` () =
    Log.Logger <-
        LoggerConfiguration()
            .WriteTo.Debug()
            .CreateLogger()

    let struct (serverStream, clientStream) = FullDuplexStream.CreatePair()

    let daemon =
        new FantomasLSPServer(serverStream, serverStream)

    let client = new JsonRpc(clientStream, clientStream)
    client.StartListening()

    async {
        do!
            client.InvokeAsync(Methods.InitializeName)
            |> Async.AwaitTask

        client.Dispose()
        (daemon :> IDisposable).Dispose()

        let reader = new StreamReader(serverStream)
        let! result = reader.ReadToEndAsync() |> Async.AwaitTask

        printfn "%s" result
        Assert.IsNotEmpty(result)
    }
    |> Async.RunSynchronously
