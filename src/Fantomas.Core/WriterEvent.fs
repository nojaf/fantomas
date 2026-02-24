namespace Fantomas.Core

/// Represents a single event emitted during the code formatting process.
/// The sequence of writer events captures how the formatter produces its output.
[<Struct>]
type WriterEvent =
    | Write of text: string
    | WriteLine
    | WriteLineInsideStringConst
    | WriteBeforeNewline of content: string
    | WriteLineBecauseOfTrivia
    | WriteLineInsideTrivia
    | IndentBy of indent: int
    | UnIndentBy of unindent: int
    | SetIndent of setIndent: int
    | RestoreIndent of restoreIndent: int
    | SetAtColumn of setAtColumn: int
    | RestoreAtColumn of restoreAtColumn: int
    | NodeStart of nodeType: string * range: string
    | NodeEnd of endNodeType: string * endRange: string
