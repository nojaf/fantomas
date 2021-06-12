module Fantomas.Tests.Fragments

open Fantomas
open System.IO
open FSharp.Compiler.SyntaxTree
open FSharp.Compiler.Text
open FSharp.Compiler.SourceCodeServices
open NUnit.Framework

let checker = FSharpChecker.Create()

let config =
    { FormatConfig.FormatConfig.Default with
          StrictMode = true }

let parsingOptions filePath =
    { FSharpParsingOptions.Default with
          SourceFiles = [| filePath |] }

type ASTFragment = Fragment of ParsedInput * Range

let updateModuleInImpl (ast: ParsedInput) (mdl: SynModuleOrNamespace) : ParsedInput =
    match ast with
    | ParsedInput.SigFile _ -> ast
    | ParsedInput.ImplFile (ParsedImplFileInput (fileName,
                                                 isScript,
                                                 qualifiedNameOfFile,
                                                 scopedPragmas,
                                                 hashDirectives,
                                                 _,
                                                 isLastAndCompiled)) ->
        ParsedImplFileInput(
            fileName,
            isScript,
            qualifiedNameOfFile,
            scopedPragmas,
            hashDirectives,
            [ mdl ],
            isLastAndCompiled
        )
        |> ParsedInput.ImplFile

let updateModuleInSig (ast: ParsedInput) (mdl: SynModuleOrNamespaceSig) : ParsedInput =
    match ast with
    | ParsedInput.ImplFile _ -> ast
    | ParsedInput.SigFile (ParsedSigFileInput (fileName, qualifiedNameOfFile, scopedPragmas, hashDirectives, _)) ->
        ParsedSigFileInput(fileName, qualifiedNameOfFile, scopedPragmas, hashDirectives, [ mdl ])
        |> ParsedInput.SigFile

let splitModule (ast: ParsedInput) (mn: SynModuleOrNamespace) : ASTFragment list =
    match mn with
    | SynModuleOrNamespace.SynModuleOrNamespace (lid, isRec, kind, decls, xmlDoc, attribs, ao, range) ->
        decls
        |> List.map
            (fun d ->
                let parsedInput =
                    SynModuleOrNamespace(lid, isRec, kind, [ d ], xmlDoc, attribs, ao, range)
                    |> updateModuleInImpl ast

                ASTFragment.Fragment(parsedInput, d.Range))

let splitModuleSig (ast: ParsedInput) (mn: SynModuleOrNamespaceSig) : ASTFragment list =
    match mn with
    | SynModuleOrNamespaceSig.SynModuleOrNamespaceSig (lid, isRec, kind, decls, xmlDoc, attribs, ao, range) ->
        decls
        |> List.map
            (fun d ->
                let parsedInput =
                    SynModuleOrNamespaceSig(lid, isRec, kind, [ d ], xmlDoc, attribs, ao, range)
                    |> updateModuleInSig ast

                ASTFragment.Fragment(parsedInput, d.Range))

let splitParsedInput (ast: ParsedInput) : ASTFragment list =
    match ast with
    | ParsedInput.ImplFile (ParsedImplFileInput.ParsedImplFileInput (modules = modules)) ->
        modules |> List.collect (splitModule ast)
    | ParsedInput.SigFile (ParsedSigFileInput.ParsedSigFileInput (modules = modules)) ->
        modules |> List.collect (splitModuleSig ast)

let formatFragment
    (lines: string array)
    (defines: string list)
    (Fragment (ast, range))
    (fileName: string)
    : Async<unit> =
    async {
        try
            let! formatted = CodeFormatter.FormatASTAsync(ast, fileName, defines, None, config)
            File.WriteAllText(fileName, formatted)
        with
        | ex ->
            let errorFile =
                let name =
                    Path.GetFileNameWithoutExtension(fileName)
                    |> sprintf "%s_error.txt"

                Path.Combine(Path.GetDirectoryName(fileName), name)

            let code =
                lines.[(range.StartLine - 1)..(range.EndLine - 1)]
                |> String.concat "\n"

            let errorLog =
                $"""Unable to format %s{fileName}

Original range: %A{range}

Code found for this range:

%s{code}

Exception: %A{ex}
    """

            File.WriteAllText(errorFile, errorLog)
    }

let formatFragments (filePath: string) : unit =
    let extension = Path.GetExtension(filePath)

    let fileNameWithoutExtension =
        Path.GetFileNameWithoutExtension(filePath)

    let sourceOrigin =
        File.ReadAllText filePath
        |> SourceOrigin.SourceString

    let lines = File.ReadAllLines filePath

    let ast =
        CodeFormatter.ParseAsync(filePath, sourceOrigin, parsingOptions filePath, checker)
        |> Async.RunSynchronously

    let fragments : (string list * ASTFragment list) list =
        ast
        |> Seq.map (fun (ast, defines) -> defines, splitParsedInput ast)
        |> Seq.toList

    let fragmentFolder =
        Path.GetFullPath filePath
        |> sprintf "%s_format_results"

    if not (Directory.Exists(fragmentFolder)) then
        Directory.CreateDirectory(fragmentFolder)
        |> ignore

    let formatFragments folder defines fragments =
        let fragmentFileName idx =
            Path.Combine(folder, $"%s{fileNameWithoutExtension}_%04i{idx}%s{extension}")

        fragments
        |> List.mapi (fun idx fragment -> formatFragment lines defines fragment (fragmentFileName idx))
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

    match fragments with
    | [] -> ()
    | [ [], fragments ] -> formatFragments fragmentFolder [] fragments
    | fragmentsWithDefines ->
        fragmentsWithDefines
        |> List.iter
            (fun (defines, fragments) ->
                let folder =
                    let suffix = String.concat "_" defines
                    Path.Combine(fragmentFolder, suffix)

                if not (Directory.Exists(folder)) then
                    Directory.CreateDirectory(folder) |> ignore

                formatFragments folder defines fragments)



[<Test>]
let ``format in fragments`` () =
    formatFragments @"C:\Users\nojaf\Projects\fsharp\src\fsharp\FSharp.Core\math\z.fs"

let private validate (fileName: string) (code: string) : Async<FSharpDiagnostic list> =
    let options =
        { FSharpParsingOptions.Default with
              SourceFiles = [| fileName |] }

    let sourceCode = SourceText.ofString code

    async {
        let! result = checker.ParseFile(fileName, sourceCode, options)

        return result.Errors |> Array.toList
    }

let formatPerDefines (filePath: string) =
    let extension = Path.GetExtension(filePath)

    let fileNameWithoutExtension =
        Path.GetFileNameWithoutExtension(filePath)

    let fragmentFolder =
        Path.GetFullPath filePath
        |> sprintf "%s_format_results"

    if not (Directory.Exists(fragmentFolder)) then
        Directory.CreateDirectory(fragmentFolder)
        |> ignore

    let sourceOrigin =
        File.ReadAllText filePath
        |> SourceOrigin.SourceString

    let config = FormatConfig.FormatConfig.Default

    let astAndDefines =
        CodeFormatter.ParseAsync(filePath, sourceOrigin, parsingOptions filePath, checker)
        |> Async.RunSynchronously

    astAndDefines
    |> Array.map
        (fun (ast, defines) ->
            async {
                let resultFileName =
                    String.concat "_" defines
                    |> fun d -> $"%s{fileNameWithoutExtension}_%s{d}%s{extension}"

                let outputPath =
                    Path.Combine(fragmentFolder, resultFileName)

                let! result = CodeFormatter.FormatASTAsync(ast, filePath, defines, Some sourceOrigin, config)

                do!
                    File.WriteAllTextAsync(outputPath, result)
                    |> Async.AwaitTask

                let! validationErrors = validate outputPath result

                if List.isNotEmpty validationErrors then
                    let errorFileName =
                        String.concat "_" defines
                        |> fun d -> $"%s{fileNameWithoutExtension}_%s{d}_errors.txt"
                        |> fun f -> Path.Combine(fragmentFolder, f)
                        
                    File.WriteAllText(errorFileName, sprintf "%A" validationErrors)
            }
            |> Async.Catch)
        |> Async.Parallel
        |> Async.Ignore
        |> Async.RunSynchronously

[<Test>]
let ``format per define combo ziggy`` () =
    [|
        @"C:\Users\nojaf\Projects\fsharp\src\fsharp\utils\prim-parsing.fs"
    |]
    |> Array.map (fun f -> async {
        try
            formatPerDefines f
        with
        | ex ->
            printfn $"failure %A{ex} in %s{f}"
    })
    |> Async.Parallel
    |> Async.Ignore
