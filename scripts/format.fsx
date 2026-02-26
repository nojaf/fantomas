#load "shared.fsx"

open System.IO
open Fantomas.Core
open Shared

let format (input: string) (isSignature: bool) (config: FormatConfig) =
    async {
        try
            let! result = CodeFormatter.FormatDocumentAsync(isSignature, input, config)
            return result.Code
        with ex ->
            return $"Error while formatting: %A{ex}"
    }

match Array.tryHead fsi.CommandLineArgs with
| Some scriptPath ->
    let scriptFile = FileInfo(scriptPath)
    let sourceFile = FileInfo(Path.Combine(__SOURCE_DIRECTORY__, __SOURCE_FILE__))

    if scriptFile.FullName = sourceFile.FullName then
        let sample, isSignature, config = parseArgs fsi.CommandLineArgs.[1..]
        format sample isSignature config |> Async.RunSynchronously |> printfn "%s"
| _ -> printfn "Usage: dotnet fsi format.fsx [--editorconfig <content>] <input file>"
