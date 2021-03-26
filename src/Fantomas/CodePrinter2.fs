module Fantomas.CodePrinter2

open System
open System.Text.RegularExpressions
open FSharp.Compiler.Text.Range
open FSharp.Compiler.Text.Pos
open FSharp.Compiler.SourceCodeServices
open FSharp.Compiler.SyntaxTree
open FSharp.Compiler.Text
open Fantomas
open Fantomas.FormatConfig
open Fantomas.SourceOrigin
open Fantomas.SourceTransformer
open Fantomas.CodePrinter
open Fantomas.TriviaTypes
open Fantomas.AstExtensions

type CodePrinterInfo =
    { HashTokens: Token list
      Defines: string list
      SourceCodeLines: string array
      FileName: string
      Newline: string
      Config: FormatConfig }

type FormattedSourceCodeUnit =
    { Source: string
      OriginalRange: Range
      IsModuleName: bool }

    static member Create (source: string) (originalRange: Range) (isModule: bool) : FormattedSourceCodeUnit =
        { Source = source
          OriginalRange = originalRange
          IsModuleName = isModule }

let private hashTokenRegex = Regex("^ *#if\s\w+")

let private prependNewline (codePrinterInfo: CodePrinterInfo) (v: string) : string =
    String.Concat(codePrinterInfo.Newline, v)

let private formatModuleDeclaration
    (codePrinterInfo: CodePrinterInfo)
    (decl: SynModuleDecl)
    : Async<FormattedSourceCodeUnit> =
    async {
        let source =
            codePrinterInfo.SourceCodeLines.[(decl.FullRange.StartLine - 1)..(decl.Range.EndLine - 1)]

        let ctx =
            Context.Context.Create
                codePrinterInfo.Config
                codePrinterInfo.Defines
                codePrinterInfo.FileName
                codePrinterInfo.HashTokens
                source
                (TriviaCollectionStartInfo.ModuleDeclaration decl)

        let fragment =
            genModuleDecl ASTContext.Default decl ctx
            |> Context.dump

        return FormattedSourceCodeUnit.Create fragment decl.FullRange false
    }

let private formatSignatureDeclaration
    (codePrinterInfo: CodePrinterInfo)
    (sigDecl: SynModuleSigDecl)
    : Async<FormattedSourceCodeUnit> =
    async {
        let source =
            codePrinterInfo.SourceCodeLines.[(sigDecl.Range.StartLine - 1)..(sigDecl.Range.EndLine - 1)]

        let ctx =
            Context.Context.Create
                codePrinterInfo.Config
                codePrinterInfo.Defines
                codePrinterInfo.FileName
                codePrinterInfo.HashTokens
                source
                (TriviaCollectionStartInfo.SignatureDeclaration sigDecl)

        let fragment =
            genSigModuleDecl ASTContext.Default sigDecl ctx
            |> Context.dump

        return FormattedSourceCodeUnit.Create fragment sigDecl.Range false
    }

let private getContentBetweenExpressions (codePrinterInfo: CodePrinterInfo) (r1: Range) (r2: Range) : string option =
    let endLineFirst = r1.EndLine
    let startLineLast = r2.StartLine
    let distance = Math.Abs(endLineFirst - startLineLast)

    if distance > 1 then
        let originalSource =
            codePrinterInfo.SourceCodeLines.[endLineFirst..(startLineLast - 2)]

        let containsIfHash =
            Array.exists hashTokenRegex.IsMatch originalSource

        if containsIfHash then
            // replace dead code with empty strings.
            let linesThatProducesTokens =
                TokenParser.tokenize codePrinterInfo.Defines [] endLineFirst originalSource
                |> List.map (fun t -> t.LineNumber)
                |> List.distinct

            originalSource
            |> Array.mapi
                (fun idx line ->
                    let lineNumber = idx + endLineFirst

                    if List.contains lineNumber linesThatProducesTokens then
                        line
                    else
                        String.Empty)
            |> String.concat codePrinterInfo.Newline
            |> Some
        else
            originalSource
            |> String.concat codePrinterInfo.Newline
            |> Some
    else
        None

let combineFormattedResults
    /// In some scenarios you want to prepend a newline (for example between multiline let binding and following type)
    /// In other cases, you don't (for example multiline attributes and unit expression)
    (betweenMultilineBlocks: CodePrinterInfo -> string -> string)
    (codePrinterInfo: CodePrinterInfo)
    (formattedSourceCodeUnits: (FormattedSourceCodeUnit) array)
    : string =
    let file =
        ResizeArray<string>(formattedSourceCodeUnits.Length * 2 + 1)

    let appendToFile : string -> unit = file.Add

    let declIsMultiline index =
        let ({ Source = source }: FormattedSourceCodeUnit) = formattedSourceCodeUnits.[index]
        source.Contains(codePrinterInfo.Newline)

    Array.iteri
        (fun idx { Source = decl
                   OriginalRange = r
                   IsModuleName = isModule } ->
            let contentBetweenExpressions =
                if idx = 0 then
                    None
                else
                    let { OriginalRange = lastDeclRange } = formattedSourceCodeUnits.[idx - 1]
                    getContentBetweenExpressions codePrinterInfo lastDeclRange r

            if idx > 0 then
                // print content between last and current decl
                Option.iter appendToFile contentBetweenExpressions

            let firstDeclWithoutContentBetweenModuleName =
                idx = 1
                && let { IsModuleName = isModule } = formattedSourceCodeUnits.[idx - 1] in

                   isModule
                   && Option.isNone contentBetweenExpressions

            let noContentBetweenExpressions = Option.isNone contentBetweenExpressions

            let multilineDeclWithoutContentBetweenPreviousDecl () =
                idx > 0
                && noContentBetweenExpressions
                && not isModule
                && declIsMultiline idx

            let previousDeclIsMultilineAndNoContentBetweenPreviousDecl () =
                idx > 0
                && noContentBetweenExpressions
                && declIsMultiline (idx - 1)

            if firstDeclWithoutContentBetweenModuleName then
                // add an extra newline between the module name and the first decl
                String.Concat(codePrinterInfo.Newline, decl)
                |> appendToFile
            elif multilineDeclWithoutContentBetweenPreviousDecl ()
                 || previousDeclIsMultilineAndNoContentBetweenPreviousDecl () then
                // current decl is multiline or previous is multiline and there is no content in between with the last one
                betweenMultilineBlocks codePrinterInfo decl
                |> appendToFile
            else
                appendToFile decl)
        formattedSourceCodeUnits

    String.concat codePrinterInfo.Newline file

let private formatModule
    (codePrinterInfo: CodePrinterInfo)
    (kind: SynModuleOrNamespaceKind)
    (longId: LongIdent)
    (ao: SynAccess option)
    (isRecursive: bool)
    (attrs: SynAttributes)
    (firstDeclRange: Range option)
    (lastDeclRange: Range option)
    (declExpressions: Async<FormattedSourceCodeUnit> list)
    (moduleRange: Range)
    : Async<FormattedSourceCodeUnit> =
    // TODO: move correctedRange workaround in next FCS version
    let (correctedRange: Range, moduleName: Async<FormattedSourceCodeUnit> option) =
        match kind with
        | SynModuleOrNamespaceKind.NamedModule
        | SynModuleOrNamespaceKind.DeclaredNamespace
        | SynModuleOrNamespaceKind.GlobalNamespace ->
            let tokens =
                let firstDeclHeadLine =
                    firstDeclRange
                    |> Option.map (fun r -> r.StartLine)
                    |> Option.defaultValue (codePrinterInfo.SourceCodeLines.Length)
                    |> (+) -2 // -1 for sourceCodeLines zero based, -1 to get line before decl

                let source =
                    codePrinterInfo.SourceCodeLines.[0..firstDeclHeadLine]

                TokenParser.tokenize codePrinterInfo.Defines codePrinterInfo.HashTokens 1 source

            let range =
                let startPos =
                    let namespaceOrModuleToken =
                        tokens
                        |> List.skipWhile
                            (fun t ->
                                t.TokenInfo.TokenName <> "NAMESPACE"
                                && t.TokenInfo.TokenName <> "MODULE"
                                && t.TokenInfo.TokenName <> "LBRACK_LESS") // start of attribute
                        |> List.head

                    mkPos namespaceOrModuleToken.LineNumber namespaceOrModuleToken.TokenInfo.LeftColumn

                let endPos =
                    List.tryLast longId
                    |> Option.map (fun l -> l.idRange.End)
                    |> Option.defaultWith
                        (fun () ->
                            tokens
                            |> List.skipWhile (fun t -> t.TokenInfo.TokenName <> "GLOBAL")
                            |> List.head
                            |> fun t -> mkPos t.LineNumber t.TokenInfo.RightColumn)

                mkRange codePrinterInfo.FileName startPos endPos

            let correctedModuleRange =
                match lastDeclRange with
                | Some ldr -> mkRange codePrinterInfo.FileName range.Start ldr.End
                | None -> range

            let source =
                codePrinterInfo.SourceCodeLines.[(range.StartLine - 1)..(range.EndLine - 1)]

            let ctx =
                Context.Context.Create
                    codePrinterInfo.Config
                    codePrinterInfo.Defines
                    codePrinterInfo.FileName
                    codePrinterInfo.HashTokens
                    source
                    (TriviaCollectionStartInfo.NamespaceOrModule(longId, range, tokens))

            let fragment =
                genModuleName ASTContext.Default kind isRecursive ao longId attrs ctx
                |> Context.dump

            let formatTask =
                FormattedSourceCodeUnit.Create fragment range true
                |> async.Return
                |> Some

            correctedModuleRange, formatTask
        | SynModuleOrNamespaceKind.AnonModule ->
            let correctedRange =
                match firstDeclRange, lastDeclRange with
                | Some rf, Some rl -> mkRange codePrinterInfo.FileName rf.Start rl.End
                | _ -> moduleRange

            correctedRange, None

    let topLevelExpressions =
        match moduleName with
        | Some mn -> mn :: declExpressions
        | None -> declExpressions

    topLevelExpressions
    |> Async.Parallel
    |> Async.map
        (fun results ->
            let source =
                combineFormattedResults prependNewline codePrinterInfo results

            FormattedSourceCodeUnit.Create source correctedRange false)

let private combineGroupedDecls
    (codePrinterInfo: CodePrinterInfo)
    (decls: SynModuleDecl list)
    : Async<FormattedSourceCodeUnit> =
    List.map (formatModuleDeclaration codePrinterInfo) decls
    |> Async.Parallel
    |> Async.map
        (fun results ->
            let range =
                match Array.tryHead results, Array.tryLast results with
                | Some { OriginalRange = rs }, Some { OriginalRange = re } -> mkRange rs.FileName rs.Start re.End
                | _ -> Range.Zero

            let source =
                combineFormattedResults (fun _ -> id) codePrinterInfo results

            FormattedSourceCodeUnit.Create source range false)

let rec private collectSynModuleDeclGroups
    (codePrinterInfo: CodePrinterInfo)
    (decls: SynModuleDecl list)
    (finalContinuation: Async<FormattedSourceCodeUnit> list -> Async<FormattedSourceCodeUnit> list)
    : Async<FormattedSourceCodeUnit> list =
    match decls with
    | [] -> finalContinuation []
    | AttributesLDoExprUnit (xs, ys)
    | OpenL (xs, ys)
    | HashDirectiveL (xs, ys) ->
        collectSynModuleDeclGroups
            codePrinterInfo
            ys
            (fun rest ->
                (combineGroupedDecls codePrinterInfo xs) :: rest
                |> finalContinuation)
    | h :: rest ->
        collectSynModuleDeclGroups
            codePrinterInfo
            rest
            (fun rest ->
                formatModuleDeclaration codePrinterInfo h :: rest
                |> finalContinuation)

let formatWith
    (ast: ParsedInput)
    (defines: string list)
    (hashTokens: Token list)
    (formatContext: FormatContext)
    (config: FormatConfig)
    : Async<string> =
    let codePrinterInfo =
        { HashTokens = hashTokens
          Defines = defines
          SourceCodeLines = String.normalizeThenSplitNewLine formatContext.Source
          FileName = formatContext.FileName
          Newline = config.EndOfLine.NewLineString
          Config = config }

    match ast with
    | ParsedInput.ImplFile (ParsedImplFileInput.ParsedImplFileInput (modules = modules)) ->
        List.map
            (fun (SynModuleOrNamespace (longIdent, isRecursive, kind, decls, xml, attrs, ao, range)) ->
                let firstDeclRange =
                    List.tryHead decls
                    |> Option.map (fun d -> d.FullRange)

                let lastDeclRange =
                    List.tryLast decls
                    |> Option.map (fun d -> d.Range)

                let declExpressions =
                    collectSynModuleDeclGroups codePrinterInfo decls id

                formatModule
                    codePrinterInfo
                    kind
                    longIdent
                    ao
                    isRecursive
                    attrs
                    firstDeclRange
                    lastDeclRange
                    declExpressions
                    range)
            modules
    | ParsedInput.SigFile (ParsedSigFileInput.ParsedSigFileInput (modules = modules)) ->
        List.map
            (fun (SynModuleOrNamespaceSig (longIdent, isRecursive, kind, sigDecls, xml, attrs, ao, range)) ->
                let firstSigDeclRange =
                    List.tryHead sigDecls
                    |> Option.map (fun sd -> sd.Range)

                let lastSigDeclRange =
                    List.tryLast sigDecls
                    |> Option.map (fun d -> d.Range)

                let sigDeclExpressions =
                    List.map (formatSignatureDeclaration codePrinterInfo) sigDecls

                formatModule
                    codePrinterInfo
                    kind
                    longIdent
                    ao
                    isRecursive
                    attrs
                    firstSigDeclRange
                    lastSigDeclRange
                    sigDeclExpressions
                    range)
            modules
    |> Async.Parallel
    |> Async.map
        (fun formattedModules ->
            let file =
                ResizeArray<string>(formattedModules.Length * 2 + 1)

            let appendToFile : string -> unit = file.Add

            match Array.tryHead formattedModules with
            | Some { OriginalRange = range } ->
                let startOfFile =
                    mkRange codePrinterInfo.FileName (mkPos 0 0) (mkPos 0 0)

                getContentBetweenExpressions codePrinterInfo startOfFile range
                |> Option.iter
                    (fun leading ->
                        if not (String.IsNullOrWhiteSpace(leading)) then
                            appendToFile (leading.TrimStart('\r', '\n')))
            | _ -> ()

            combineFormattedResults prependNewline codePrinterInfo formattedModules
            |> appendToFile

            match Array.tryLast formattedModules with
            | Some { OriginalRange = range } ->
                let lastPosition =
                    mkPos (codePrinterInfo.SourceCodeLines.Length + 1) 0

                let endOfFileRange =
                    mkRange codePrinterInfo.FileName lastPosition lastPosition

                getContentBetweenExpressions codePrinterInfo range endOfFileRange
                |> Option.iter
                    (fun content ->
                        if not (String.IsNullOrWhiteSpace(content)) then
                            content.TrimEnd('\r', '\n') |> appendToFile)
            | _ -> ()

            // always end with a blank line
            let lastIndex = file.Count - 1

            if not (file.[lastIndex].EndsWith(codePrinterInfo.Newline)) then
                file.[lastIndex] <- String.Concat(file.[lastIndex], codePrinterInfo.Newline)

            String.concat codePrinterInfo.Newline file)
