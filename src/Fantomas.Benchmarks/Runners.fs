module Fantomas.Benchmarks.Runners

open BenchmarkDotNet.Attributes
open System.IO
open FSharp.Compiler.SourceCodeServices
open Fantomas
open Fantomas.Extras

let config = FormatConfig.FormatConfig.Default

[<MemoryDiagnoser>]
[<RankColumn>]
type CodePrinterTest() =
    [<Benchmark>]
    member _.Format() =
        let path =
            __SOURCE_DIRECTORY__
            + "/../../paket-files/fsprojects/fantomas/src/Fantomas/CodePrinter.fs"

        let fileName = Path.GetFileName(path)

        let content =
            File.ReadAllText(path)
            |> SourceOrigin.SourceString

        CodeFormatter.FormatDocumentAsync(fileName, content, config)
        |> Async.RunSynchronously
        |> ignore

        ()
