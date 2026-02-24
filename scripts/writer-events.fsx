#r "../artifacts/bin/Fantomas.FCS/debug/Fantomas.FCS.dll"
#r "../artifacts/bin/Fantomas.Core/debug/Fantomas.Core.dll"

open System.IO
open Fantomas.Core

let getWriterEvents (input: string) (isSignature: bool) =
    async {
        try
            let! events = CodeFormatter.GetWriterEventsAsync(isSignature, input)
            return events |> Array.map string |> String.concat "\n"
        with ex ->
            return $"Error while getting writer events: %A{ex}"
    }

match Array.tryHead fsi.CommandLineArgs with
| Some scriptPath ->
    let scriptFile = FileInfo(scriptPath)
    let sourceFile = FileInfo(Path.Combine(__SOURCE_DIRECTORY__, __SOURCE_FILE__))

    if scriptFile.FullName = sourceFile.FullName then
        let inputPath = fsi.CommandLineArgs.[fsi.CommandLineArgs.Length - 1]
        let sample = File.ReadAllText(inputPath)
        let isSignature = inputPath.EndsWith(".fsi")

        getWriterEvents sample isSignature |> Async.RunSynchronously |> printfn "%s"
| _ -> printfn "Usage: dotnet fsi writer-events.fsx <input file>"
