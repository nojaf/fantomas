#r "../artifacts/bin/Fantomas.FCS/debug/Fantomas.FCS.dll"
#r "../artifacts/bin/Fantomas.Core/debug/Fantomas.Core.dll"
#r "nuget: editorconfig"

#load "../src/Fantomas/EditorConfig.fs"

open System.IO
open Fantomas.Core
open Fantomas.EditorConfig

let parseEditorConfigContent (content: string) : FormatConfig =
    let tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
    Directory.CreateDirectory(tempDir) |> ignore
    let editorConfigPath = Path.Combine(tempDir, ".editorconfig")
    let fsharpFile = Path.Combine(tempDir, "temp.fs")
    File.WriteAllText(editorConfigPath, $"root = true\n\n[*.fs]\n%s{content}")
    File.WriteAllText(fsharpFile, "")

    try
        readConfiguration fsharpFile
    finally
        Directory.Delete(tempDir, true)

let parseArgs (args: string array) =
    let editorConfigIdx = args |> Array.tryFindIndex (fun a -> a = "--editorconfig")

    let config, inputPath =
        match editorConfigIdx with
        | Some idx ->
            let editorConfigContent = args.[idx + 1]
            let config = parseEditorConfigContent editorConfigContent
            config, args.[args.Length - 1]
        | None -> FormatConfig.Default, args.[args.Length - 1]

    let sample = File.ReadAllText(inputPath)
    let isSignature = inputPath.EndsWith(".fsi")
    sample, isSignature, config
