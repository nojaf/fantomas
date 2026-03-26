namespace Fantomas.Core

/// Represents a single event emitted during the code formatting process.
/// The sequence of writer events captures how the formatter produces its output.
[<Struct>]
type WriterEvent =
    | Write of text: string
    /// Emits text that originated from a comment (line comment, block comment, or XML doc line).
    /// Distinguishing comments from regular code writes allows the formatting engine to recognise
    /// comment events without fragile string-prefix checks (e.g. "starts with //").
    /// This matters in Context.fs where we decide whether an event is trivia (and therefore does
    /// not count towards multiline detection) or real output.
    | WriteComment of text: string
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
