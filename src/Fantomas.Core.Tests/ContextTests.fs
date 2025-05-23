module Fantomas.Core.Tests.ContextTests

open NUnit.Framework
open FsUnit
open Fantomas.Core.Context
open Fantomas.Core.Tests.TestHelpers
open Fantomas.Core

let private dump ctx = (dump false ctx).Code

[<Test>]
let ``sepSpace should not add an additional space if the line ends with a space`` () =
    let expr = !-"let a = " +> sepSpace
    let result = dump (expr Context.Default)
    result |> should equal "let a ="

[<Test>]
let ``sepColon should not add a space when nothing proceeds it`` () =
    let expr =
        !-"let add a b"
        +> indent
        +> sepNln
        +> sepColon
        +> !-"int ="
        +> indent
        +> sepNln
        +> !-"a + b"
        +> unindent
        +> unindent
        +> sepNln

    let config =
        { FormatConfig.Default with
            SpaceBeforeColon = true }

    let ctx = { Context.Default with Config = config }
    let result = dump (expr ctx)

    result
    |> prepend newline
    |> String.normalizeNewLine
    |> should
        equal
        """
let add a b
    : int =
        a + b
"""

[<Test>]
let ``sepColon should not add a space when space proceeds it`` () =
    let expr = !-"let a " +> sepNone +> sepColon

    let config =
        { FormatConfig.Default with
            SpaceBeforeColon = true }

    let ctx = { Context.Default with Config = config }
    let result = dump (expr ctx)
    result |> should equal "let a :"

[<Test>]
let ``don't add space before block comment`` () =
    let comment =
        """(*

Long comment

*)"""
        |> String.normalizeNewLine

    let expr = sepNone +> sepSpace +> !-comment +> sepSpace +> sepNone
    let result = dump (expr Context.Default)

    result
    |> prepend newline
    |> String.normalizeNewLine
    |> should
        equal
        """
(*

Long comment

*)"""

[<Test>]
let ``nested exceedsMultiline expression should bubble up to parent check`` () =
    let expression =
        !-"let a ="
        +> autoIndentAndNlnIfExpressionExceedsPageWidth (
            expressionFitsOnRestOfLine
                // short expression, should cross the max page width of 50
                (!-"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +> !-"BBBBBBBBBBBBBBBBBBBBBBBBBBBBBB")
                // fallback expression
                (!-"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
                 +> sepNln
                 +> !-"BBBBBBBBBBBBBBBBBBBBBBBBBBBBBB"
                 +> sepNln)
        )

    let config =
        { FormatConfig.Default with
            MaxLineLength = 50
            SpaceAroundDelimiter = false }

    let initialContext = { Context.Default with Config = config }

    let result = dump (expression initialContext)

    result
    |> prepend newline
    |> String.normalizeNewLine
    |> should
        equal
        """
let a =
    AAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
    BBBBBBBBBBBBBBBBBBBBBBBBBBBBBB
"""
