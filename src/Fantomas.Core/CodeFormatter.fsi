namespace Fantomas.Core

open Fantomas.Core.FormatConfig
open Fantomas.Core.SyntaxOak
open FSharp.Compiler.Text
open FSharp.Compiler.Syntax

[<Sealed>]
type CodeFormatter =
    // /// Parse a source string using given config
    static member ParseAsync: isSignature: bool * source: string -> Async<(ParsedInput * string list) array>

    /// Format an abstract syntax tree using an optional source for trivia processing
    static member FormatASTAsync: ast: ParsedInput * ?source: string * ?config: FormatConfig -> Async<string>

    /// Format a source string using an optional config
    static member FormatDocumentAsync: isSignature: bool * source: string * ?config: FormatConfig -> Async<string>

    // /// Format a part of source string using given config, and return the (formatted) selected part only.
    // /// Beware that the range argument is inclusive. The closest expression inside the selection will be formatted if possible.
    static member FormatSelectionAsync:
        isSignature: bool * source: string * selection: Range * ?config: FormatConfig -> Async<string * range>

    // /// Check whether an input string is invalid in F# by attempting to parse the code.
    static member IsValidFSharpCodeAsync: isSignature: bool * source: string -> Async<bool>

    /// Returns the version of Fantomas found in the AssemblyInfo
    static member GetVersion: unit -> string

    /// Make a range from (startLine, startCol) to (endLine, endCol) to select some text
    static member MakeRange: fileName: string * startLine: int * startCol: int * endLine: int * endCol: int -> range

    [<Experimental "Only for local development">]
    static member ParseOakAsync:
        isSignature: bool * source: string * ?config: FormatConfig -> Async<(Oak * string list) array>

    [<Experimental "Only for local development">]
    static member FormatOakAsync: oak: Oak * ?config: FormatConfig -> Async<string>
