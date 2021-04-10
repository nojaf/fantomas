namespace Fantomas

[<Sealed>]
type CodeFormatter =
    static member ParseAsync(fileName, source) =
        async {
            let! asts =
                CodeFormatterImpl.createFormatContext fileName source
                |> CodeFormatterImpl.parse

            return (Array.map (fun (a, d, _) -> a, d) asts)
        }

    static member FormatASTAsync(ast, fileName, defines, source, config) =
        let formatContext =
            CodeFormatterImpl.createFormatContext fileName (Option.defaultValue (SourceOrigin.SourceString "") source)

        CodeFormatterImpl.formatAST ast defines formatContext config
        |> async.Return

    static member FormatDocumentAsync(fileName, source, config) =
        CodeFormatterImpl.createFormatContext fileName source
        |> CodeFormatterImpl.formatDocument config

    static member FormatSelectionAsync(fileName, selection, source, config) =
        CodeFormatterImpl.createFormatContext fileName source
        |> CodeFormatterImpl.formatSelection selection config

    static member IsValidFSharpCodeAsync(fileName, source) =
        CodeFormatterImpl.createFormatContext fileName source
        |> CodeFormatterImpl.isValidFSharpCode

    static member IsValidASTAsync ast =
        async { return CodeFormatterImpl.isValidAST ast }

    static member MakePos(line, col) = CodeFormatterImpl.makePos line col

    static member MakeRange(fileName, startLine, startCol, endLine, endCol) =
        CodeFormatterImpl.makeRange fileName startLine startCol endLine endCol

    static member GetVersion() = Version.fantomasVersion.Value
