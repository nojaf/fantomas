module Fantomas.CoreGlobalTool.Tests.DaemonTests

open System
open Fantomas.CoreGlobalTool.Daemon
open LspTypes
open NUnit.Framework
open Nerdbank.Streams
open Serilog
open StreamJsonRpc
open Fantomas
open FsUnit

Log.Logger <-
    LoggerConfiguration()
        .WriteTo.Debug()
        .CreateLogger()

// TODO: Consider Ply

let private connectToServer (callback: JsonRpc -> Async<unit>) =
    async {
        let struct (serverStream, clientStream) = FullDuplexStream.CreatePair()

        let daemon =
            new FantomasLSPServer(serverStream, serverStream)

        let client = new JsonRpc(clientStream, clientStream)
        // TODO: maybe, consider custom serializer
        client.StartListening()

        do! callback client

        client.Dispose()
        (daemon :> IDisposable).Dispose()
    }

let private shouldEqualWithPrependNewline (expected: string) (actual: string) : unit =
    String.normalizeNewLine expected
    |> should equal (String.Concat("\n", String.normalizeNewLine actual))

[<Test>]
let ``client can connect to daemon`` () =
    connectToServer
        (fun client ->
            async {
                let! result =
                    client.InvokeAsync<InitializeResult>(Methods.InitializeName)
                    |> Async.AwaitTask

                do!
                    client.InvokeAsync(Methods.InitializedName)
                    |> Async.AwaitTask

                Assert.IsNotNull(result)
                Assert.IsNotNull(result.Capabilities.DocumentFormattingProvider.Value)
            })

[<Test>]
let ``client can format full document`` () =
    connectToServer
        (fun client ->
            async {
                let source = "let foo =       42"

                let options : FormatDocumentOptions =
                    { SourceCode = source
                      Config = null
                      TextDocument = TextDocumentIdentifier(Uri = "file:///src/fs.fsx") }

                let! result =
                    client.InvokeWithParameterObjectAsync<TextEdit>("fantomas/formatSource", options)
                    |> Async.AwaitTask

                result.NewText
                |> shouldEqualWithPrependNewline
                    """
let foo = 42
"""
            })

[<Test>]
let ``client ask current version`` () =
    connectToServer
        (fun client ->
            async {
                let! result =
                    client.InvokeAsync<VersionResult>("fantomas/version")
                    |> Async.AwaitTask

                result.Version
                |> should equal (CodeFormatter.GetVersion())
            })
