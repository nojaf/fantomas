module Fantomas.CoreGlobalTool.Tests.DaemonTests

open System
open System.Text
open System.IO
open Fantomas.CoreGlobalTool.Daemon
open NUnit.Framework
open Serilog

[<Test>]
let ``client can connect to daemon`` () =
    Log.Logger <-
        LoggerConfiguration().WriteTo.Debug().CreateLogger()
    
    let input = new MemoryStream()
    let output = new MemoryStream()
    let daemon = new FantomasLSPServer(output, input)

    let initializeMessage =
        let header =
            "Content-Type: application/vscode-jsonrpc; charset=utf-8"

        let body = """{
                        "jsonrpc": "2.0",
                        "id": 1,
                        "method": "initialize"
                    }"""
        sprintf "%s\r\n\r\n%s" header body
        |> Encoding.UTF8.GetBytes

    async {
        do! (input.WriteAsync(initializeMessage, 0, initializeMessage.Length) |> Async.AwaitTask)
        do! (input.FlushAsync() |> Async.AwaitTask)
        (daemon :> IDisposable).Dispose()
        let result = Encoding.UTF8.GetString(output.ToArray())
        Assert.IsNotEmpty(result)
    }
