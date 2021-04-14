module Fantomas.CoreGlobalTool.Tests.DaemonTests

open System
open Fantomas.CoreGlobalTool.Daemon
open LspTypes
open NUnit.Framework
open Nerdbank.Streams
open Serilog
open StreamJsonRpc
open Fantomas
open FsUnit
open Fantomas.CoreGlobalTool.Tests.TestHelpers

Log.Logger <-
    LoggerConfiguration()
        .WriteTo.Debug()
        .CreateLogger()

// TODO: Consider Ply

let private connectToServer (callback: JsonRpc -> Async<unit>) =
    async {
        let struct (serverStream, clientStream) = FullDuplexStream.CreatePair()

        let daemon =
            new FantomasLSPServer(serverStream, serverStream)

        let client = new JsonRpc(clientStream, clientStream)
        // TODO: maybe, consider custom serializer
        client.StartListening()

        do! callback client

        client.Dispose()
        (daemon :> IDisposable).Dispose()
    }

let private shouldEqualWithPrependNewline (expected: string) (actual: string) : unit =
    String.normalizeNewLine expected
    |> should equal (String.Concat("\n", String.normalizeNewLine actual))

[<Test>]
let ``client can connect to daemon`` () =
    connectToServer
        (fun client ->
            async {
                let! result =
                    client.InvokeAsync<InitializeResult>(Methods.InitializeName)
                    |> Async.AwaitTask

                do!
                    client.InvokeAsync(Methods.InitializedName)
                    |> Async.AwaitTask

                Assert.IsNotNull(result)
                Assert.IsNotNull(result.Capabilities.DocumentFormattingProvider.Value)
            })

[<Test>]
let ``client can format full document`` () =
    connectToServer
        (fun client ->
            async {
                let source = "let foo =       42"

                let options : FormatDocumentOptions =
                    { SourceCode = source
                      Config = null
                      TextDocument = TextDocumentIdentifier(Uri = "file:///src/fs.fsx") }

                let! result =
                    client.InvokeWithParameterObjectAsync<TextEdit>("fantomas/formatDocument", options)
                    |> Async.AwaitTask

                result.NewText
                |> shouldEqualWithPrependNewline
                    """
let foo = 42
"""
            })

[<Test>]
let ``client ask current version`` () =
    connectToServer
        (fun client ->
            async {
                let! result =
                    client.InvokeAsync<VersionResult>("fantomas/version")
                    |> Async.AwaitTask

                result.Version
                |> should equal (CodeFormatter.GetVersion())
            })

[<Test>]
let ``configuration can be passed to formatDocument`` () =
    connectToServer
        (fun client ->
            async {
                let source = "let foo (a:int) : int =       42"

                let options : FormatDocumentOptions =
                    { SourceCode = source
                      Config = readOnlyDict [ "fsharp_space_before_colon", "true" ]
                      TextDocument = TextDocumentIdentifier(Uri = "file:///src/MyFile.fs") }

                let! result =
                    client.InvokeWithParameterObjectAsync<TextEdit>("fantomas/formatDocument", options)
                    |> Async.AwaitTask

                result.NewText
                |> shouldEqualWithPrependNewline
                    """
let foo (a : int) : int = 42
"""
            })

[<Test>]
let ``format signature file with formatDocument`` () =
    connectToServer
        (fun client ->
            async {
                let source =
                    """namespace Foobar

val barry :   int
"""

                let options : FormatDocumentOptions =
                    { SourceCode = source
                      Config = null
                      TextDocument = TextDocumentIdentifier(Uri = "file:///src/MySignatureFile.fsi") }

                let! result =
                    client.InvokeWithParameterObjectAsync<TextEdit>("fantomas/formatDocument", options)
                    |> Async.AwaitTask

                result.NewText
                |> shouldEqualWithPrependNewline
                    """
namespace Foobar

val barry : int
"""
            })

[<Test>]
let ``configuration options`` () =
    connectToServer
        (fun client ->
            async {
                let! result =
                    client.InvokeAsync<ConfigurationResult>("fantomas/configuration")
                    |> Async.AwaitTask

                result.MultilineFormatterTypes
                |> should equal [ "character_width"; "number_of_items" ]

                result.EndOfLineStyles
                |> should equal [ "lf"; "crlf" ]

                result.Options.["indent_size"].DefaultValue
                |> should equal "4"

                result.Options.["max_line_length"].DefaultValue
                |> should equal "120"

                result.Options.["fsharp_semicolon_at_end_of_line"]
                    .DefaultValue
                |> should equal "false"

                result.Options.["fsharp_space_before_parameter"]
                    .DefaultValue
                |> should equal "true"

                result.Options.["fsharp_space_before_lowercase_invocation"]
                    .DefaultValue
                |> should equal "true"

                result.Options.["fsharp_space_before_uppercase_invocation"]
                    .DefaultValue
                |> should equal "false"

                result.Options.["fsharp_space_before_class_constructor"]
                    .DefaultValue
                |> should equal "false"

                result.Options.["fsharp_space_before_member"]
                    .DefaultValue
                |> should equal "false"

                result.Options.["fsharp_space_before_colon"]
                    .DefaultValue
                |> should equal "false"

                result.Options.["fsharp_space_after_comma"]
                    .DefaultValue
                |> should equal "true"

                result.Options.["fsharp_space_before_semicolon"]
                    .DefaultValue
                |> should equal "false"

                result.Options.["fsharp_space_after_semicolon"]
                    .DefaultValue
                |> should equal "true"

                result.Options.["fsharp_indent_on_try_with"]
                    .DefaultValue
                |> should equal "false"

                result.Options.["fsharp_space_around_delimiter"]
                    .DefaultValue
                |> should equal "true"

                result.Options.["fsharp_max_if_then_else_short_width"]
                    .DefaultValue
                |> should equal "40"

                result.Options.["fsharp_max_infix_operator_expression"]
                    .DefaultValue
                |> should equal "50"

                result.Options.["fsharp_max_record_width"]
                    .DefaultValue
                |> should equal "40"

                result.Options.["fsharp_max_record_number_of_items"]
                    .DefaultValue
                |> should equal "1"

                result.Options.["fsharp_record_multiline_formatter"]
                    .DefaultValue
                |> should equal "character_width"

                result.Options.["fsharp_max_array_or_list_width"]
                    .DefaultValue
                |> should equal "40"

                result.Options.["fsharp_max_array_or_list_number_of_items"]
                    .DefaultValue
                |> should equal "1"

                result.Options.["fsharp_array_or_list_multiline_formatter"]
                    .DefaultValue
                |> should equal "character_width"

                result.Options.["fsharp_max_value_binding_width"]
                    .DefaultValue
                |> should equal "40"

                result.Options.["fsharp_max_function_binding_width"]
                    .DefaultValue
                |> should equal "40"

                result.Options.["fsharp_max_dot_get_expression_width"]
                    .DefaultValue
                |> should equal "50"

                result.Options.["fsharp_multiline_block_brackets_on_same_column"]
                    .DefaultValue
                |> should equal "false"

                result.Options.["fsharp_newline_between_type_definition_and_members"]
                    .DefaultValue
                |> should equal "false"

                result.Options.["fsharp_keep_if_then_in_same_line"]
                    .DefaultValue
                |> should equal "false"

                result.Options.["fsharp_max_elmish_width"]
                    .DefaultValue
                |> should equal "40"

                result.Options.["fsharp_single_argument_web_mode"]
                    .DefaultValue
                |> should equal "false"

                result.Options.["fsharp_align_function_signature_to_indentation"]
                    .DefaultValue
                |> should equal "false"

                result.Options.["fsharp_alternative_long_member_definitions"]
                    .DefaultValue
                |> should equal "false"

                result.Options.["fsharp_multi_line_lambda_closing_newline"]
                    .DefaultValue
                |> should equal "false"

                result.Options.["fsharp_disable_elmish_syntax"]
                    .DefaultValue
                |> should equal "false"

                result.Options.["fsharp_keep_indent_in_branch"]
                    .DefaultValue
                |> should equal "false"

                result.Options.["fsharp_blank_lines_around_nested_multiline_expressions"]
                    .DefaultValue
                |> should equal "true"

                result.Options.["fsharp_strict_mode"].DefaultValue
                |> should equal "false"

                result.Options.["indent_size"].Type
                |> should equal "number"

                result.Options.["max_line_length"].Type
                |> should equal "number"

                result.Options.["fsharp_semicolon_at_end_of_line"]
                    .Type
                |> should equal "boolean"

                result.Options.["fsharp_space_before_parameter"]
                    .Type
                |> should equal "boolean"

                result.Options.["fsharp_space_before_lowercase_invocation"]
                    .Type
                |> should equal "boolean"

                result.Options.["fsharp_space_before_uppercase_invocation"]
                    .Type
                |> should equal "boolean"

                result.Options.["fsharp_space_before_class_constructor"]
                    .Type
                |> should equal "boolean"

                result.Options.["fsharp_space_before_member"].Type
                |> should equal "boolean"

                result.Options.["fsharp_space_before_colon"].Type
                |> should equal "boolean"

                result.Options.["fsharp_space_after_comma"].Type
                |> should equal "boolean"

                result.Options.["fsharp_space_before_semicolon"]
                    .Type
                |> should equal "boolean"

                result.Options.["fsharp_space_after_semicolon"]
                    .Type
                |> should equal "boolean"

                result.Options.["fsharp_indent_on_try_with"].Type
                |> should equal "boolean"

                result.Options.["fsharp_space_around_delimiter"]
                    .Type
                |> should equal "boolean"

                result.Options.["fsharp_max_if_then_else_short_width"]
                    .Type
                |> should equal "number"

                result.Options.["fsharp_max_infix_operator_expression"]
                    .Type
                |> should equal "number"

                result.Options.["fsharp_max_record_width"].Type
                |> should equal "number"

                result.Options.["fsharp_max_record_number_of_items"]
                    .Type
                |> should equal "number"

                result.Options.["fsharp_record_multiline_formatter"]
                    .Type
                |> should equal "multilineFormatterType"

                result.Options.["fsharp_max_array_or_list_width"]
                    .Type
                |> should equal "number"

                result.Options.["fsharp_max_array_or_list_number_of_items"]
                    .Type
                |> should equal "number"

                result.Options.["fsharp_array_or_list_multiline_formatter"]
                    .Type
                |> should equal "multilineFormatterType"

                result.Options.["fsharp_max_value_binding_width"]
                    .Type
                |> should equal "number"

                result.Options.["fsharp_max_function_binding_width"]
                    .Type
                |> should equal "number"

                result.Options.["fsharp_max_dot_get_expression_width"]
                    .Type
                |> should equal "number"

                result.Options.["fsharp_multiline_block_brackets_on_same_column"]
                    .Type
                |> should equal "boolean"

                result.Options.["fsharp_newline_between_type_definition_and_members"]
                    .Type
                |> should equal "boolean"

                result.Options.["fsharp_keep_if_then_in_same_line"]
                    .Type
                |> should equal "boolean"

                result.Options.["fsharp_max_elmish_width"].Type
                |> should equal "number"

                result.Options.["fsharp_single_argument_web_mode"]
                    .Type
                |> should equal "boolean"

                result.Options.["fsharp_align_function_signature_to_indentation"]
                    .Type
                |> should equal "boolean"

                result.Options.["fsharp_alternative_long_member_definitions"]
                    .Type
                |> should equal "boolean"

                result.Options.["fsharp_multi_line_lambda_closing_newline"]
                    .Type
                |> should equal "boolean"

                result.Options.["fsharp_disable_elmish_syntax"]
                    .Type
                |> should equal "boolean"

                result.Options.["fsharp_keep_indent_in_branch"]
                    .Type
                |> should equal "boolean"

                result.Options.["fsharp_blank_lines_around_nested_multiline_expressions"]
                    .Type
                |> should equal "boolean"

                result.Options.["fsharp_strict_mode"].Type
                |> should equal "boolean"
            })

[<Test>]
let ``configurations can be passed to formatting`` () =
    connectToServer
        (fun client ->
            async {
                let source = "let add (a:int) (b:int): int = a +   b"

                let options : FormatDocumentOptions =
                    { SourceCode = source
                      Config = readOnlyDict [ "fsharp_space_before_colon", "true" ]
                      TextDocument = TextDocumentIdentifier(Uri = "file:///src/MyFile.fs") }

                let! result =
                    client.InvokeWithParameterObjectAsync<TextEdit>("fantomas/formatDocument", options)
                    |> Async.AwaitTask

                result.NewText
                |> should
                    equal
                    "let add (a : int) (b : int) : int = a + b
"
            })

[<Test>]
let ``.editorconfig is respected`` () =
    connectToServer
        (fun client ->
            async {
                use fileFixture =
                    new TemporaryFileCodeSample(
                        "let a  = // foo
                                                                    9"
                    )

                use _configFixture =
                    new ConfigurationFile(
                        """
[*.fs]
indent_size=2
"""
                    )

                let source =
                    System.IO.File.ReadAllText(fileFixture.Filename)

                let options : FormatDocumentOptions =
                    { SourceCode = source
                      Config = null
                      TextDocument = TextDocumentIdentifier(Uri = sprintf "file://%s" fileFixture.Filename) }

                let! result =
                    client.InvokeWithParameterObjectAsync<TextEdit>("fantomas/formatDocument", options)
                    |> Async.AwaitTask

                result.NewText
                |> shouldEqualWithPrependNewline
                    """
let a = // foo
  9
"""
            })

[<Test>]
let ``config has precedence over .editorconfig`` () =
    connectToServer
        (fun client ->
            async {
                use fileFixture =
                    new TemporaryFileCodeSample(
                        "let a  = // foo
                                                                    9"
                    )

                use _configFixture =
                    new ConfigurationFile(
                        """
[*.fs]
indent_size=2
"""
                    )

                let source =
                    System.IO.File.ReadAllText(fileFixture.Filename)

                let options : FormatDocumentOptions =
                    { SourceCode = source
                      Config = readOnlyDict [ "indent_size", "5" ]
                      TextDocument = TextDocumentIdentifier(Uri = sprintf "file://%s" fileFixture.Filename) }

                let! result =
                    client.InvokeWithParameterObjectAsync<TextEdit>("fantomas/formatDocument", options)
                    |> Async.AwaitTask

                result.NewText
                |> shouldEqualWithPrependNewline
                    """
let a = // foo
     9
"""
            })

[<Test>]
let ``range is correctly returned`` () =
    connectToServer
        (fun client ->
            async {
                let source =
                    """
let tryReadConfiguration (fsharpFile: string) : FormatConfig option =
    let editorConfigSettings : EditorConfig.Core.FileConfiguration = editorConfigParser.Parse(fileName = fsharpFile)

    if editorConfigSettings.Properties.Count = 0 then
        None
    else
        Some (parseOptionsFromEditorConfig editorConfigSettings.Properties)
"""

                let options : FormatDocumentOptions =
                    { SourceCode = source
                      Config = null
                      TextDocument = TextDocumentIdentifier(Uri = "file:///src/fs.fsx") }

                let! result =
                    client.InvokeWithParameterObjectAsync<TextEdit>("fantomas/formatDocument", options)
                    |> Async.AwaitTask

                // ranges in LSP are zero based
                // https://microsoft.github.io/language-server-protocol/specifications/specification-current/#range
                let expectedRange = Range()
                expectedRange.Start <- (Position(0u, 0u))
                expectedRange.End <- (Position(8u, 0u))

                result.Range |> should equal expectedRange
            })
