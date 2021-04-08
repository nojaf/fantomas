module Fantomas.CoreGlobalTool.Tests.DaemonTests

open System
open System.Text
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
        LoggerConfiguration().WriteTo.Debug().CreateLogger()
    
    let struct(serverStream, clientStream) = FullDuplexStream.CreatePair()
    
    let daemon = new FantomasLSPServer(serverStream, serverStream)
    let client = new JsonRpc(clientStream, clientStream)
    client.StartListening()
    
    async {
        try
            do!
                client.InvokeAsync(Methods.InitializeName)
                |> Async.AwaitTask
            
            client.Dispose()
            (daemon :> IDisposable).Dispose()

            let reader = new StreamReader(serverStream)
            let! result =
                reader.ReadToEndAsync()
                |> Async.AwaitTask

            printfn "%s" result
            Assert.IsNotEmpty(result)
        with
        | ex ->
            printfn "%A" ex
            raise ex // reraise ?
    }
    |> Async.RunSynchronously
//    
//
//    let initializeMessage =
//        let header =
//            "Content-Type: application/vscode-jsonrpc; charset=utf-8"
//
//        let body =
//            """{
//    "jsonrpc": "2.0",
//    "id": 1,
//    "method": "initialize"
//}"""
//        sprintf "%s\r\n\r\n%s" header body
//        |> Encoding.UTF8.GetBytes
//
//    async {
//        do! (input.WriteAsync(initializeMessage, 0, initializeMessage.Length) |> Async.AwaitTask)
//        do! (input.FlushAsync() |> Async.AwaitTask)
//        (daemon :> IDisposable).Dispose()
//        let result = Encoding.UTF8.GetString(output.ToArray())
//        Assert.IsNotEmpty(result)
//    }
