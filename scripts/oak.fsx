#load "shared.fsx"

open System.IO
open Fantomas.Core
open Shared

let parseOak (input: string) (isSignature: bool) =
    async {
        try
            let! oaks = CodeFormatter.ParseOakAsync(isSignature, input)

            match Array.tryHead oaks with
            | None -> return "No Oak found in input"
            | Some(oak, _) -> return (string oak)
        with ex ->
            return $"Error while parsing to Oak: %A{ex}"
    }

match Array.tryHead fsi.CommandLineArgs with
| Some scriptPath ->
    let scriptFile = FileInfo(scriptPath)
    let sourceFile = FileInfo(Path.Combine(__SOURCE_DIRECTORY__, __SOURCE_FILE__))

    if scriptFile.FullName = sourceFile.FullName then
        let sample, isSignature, _ = parseArgs fsi.CommandLineArgs.[1..]
        parseOak sample isSignature |> Async.RunSynchronously |> printfn "%s"
| _ -> printfn "Usage: dotnet fsi oak.fsx [--signature] <input file>"
