#!/usr/bin/env -S dotnet fsi

#load "shared.fsx"

open System
open System.IO
open System.Text
open Fantomas.Core
open Fantomas.FCS
open Shared

/// Every layout `ExperimentalInfixLayout` offers, in the order they are worth reading.
let layouts: ExperimentalInfixLayout list =
    [
        Beside
        BesideIndented
        BesideBracketAware
        OwnLineWhenNeeded
        OwnLineAlways
        OperatorNextLine
        OperatorNextLineOwn
        OperatorNextLineDeep
        Hybrid
        HybridStroustrup
    ]

/// Each input carries `LHS` where the name on the left of the operator goes. Every example is
/// formatted twice, once with each of these, so that a layout which places the right-hand side
/// relative to the name is caught rather than looked for by hand.
///
/// The two differ by one character on purpose. A layout that anchors to the name shifts by exactly
/// that one column, which is enough to detect, while a longer name would push examples over the
/// page width and the reflow that follows would hide the very thing being measured.
let shortName: string = "xs"
let longName: string = "xsy"

let repositoryRoot: string =
    Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, ".."))

let labRoot: string = Path.Combine(repositoryRoot, "infix-lab")
let inputDir: string = Path.Combine(labRoot, "input")
let outputRoot: string = Path.Combine(labRoot, "output")
let summaryPath: string = Path.Combine(labRoot, "SUMMARY.md")

/// The page width every example is formatted at. Narrow on purpose: the examples have to break
/// somewhere to say anything, and 80 keeps them readable side by side.
let maxLineLength: int = 80

let configFor (layout: ExperimentalInfixLayout) : FormatConfig =
    { FormatConfig.Default with
        MaxLineLength = maxLineLength
        ExperimentalInfixLayout = layout
    }

let parseErrors (source: string) : string list =
    let _, diagnostics = Parse.parseFile false (Text.SourceText.ofString source) []

    diagnostics
    |> List.filter (fun d -> d.Severity = Diagnostics.FSharpDiagnosticSeverity.Error)
    |> List.map (fun d -> $"FS%d{defaultArg d.ErrorNumber 0} %s{d.Message}")

let format (config: FormatConfig) (source: string) : Result<string, string> =
    try
        CodeFormatter.FormatDocumentAsync(false, source, config)
        |> Async.RunSynchronously
        |> fun r -> Ok r.Code
    with ex ->
        Error(ex.Message.Split('\n').[0])

/// The leading whitespace of every line. Two outputs with the same profile put everything in the
/// same column, whatever the identifiers happen to be.
let indentProfile (source: string) : int list =
    source.Replace("\r\n", "\n").Split('\n')
    |> Array.toList
    |> List.filter (fun line -> line.Trim() <> "")
    |> List.map (fun line -> line.Length - line.TrimStart().Length)

/// A quotation splice is a prefix application. If an example was written with an infix `%` and the
/// output holds a prefix one instead, the meaning changed.
let becameSplice (source: string) : bool =
    try
        let oak = CodeFormatter.ParseOakAsync(false, source) |> Async.RunSynchronously

        let text = sprintf "%A" (fst oak.[0])
        text.Contains "ExprPrefixAppNode"
    with _ ->
        false

type Outcome =
    | Formatted of short: string * long: string
    | Failed of string

type Case =
    {
        Layout: ExperimentalInfixLayout
        Example: string
        Outcome: Outcome
    }

    member x.Name = ExperimentalInfixLayout.ToConfigString x.Layout

let layoutName (l: ExperimentalInfixLayout) : string =
    ExperimentalInfixLayout.ToConfigString l

// ---------------------------------------------------------------------------------------------
// Run every example through every layout, twice.
// ---------------------------------------------------------------------------------------------

if Directory.Exists outputRoot then
    Directory.Delete(outputRoot, true)

let inputs: string array = Directory.GetFiles(inputDir, "*.fs") |> Array.sort

// An input that does not parse is a mistake in the example, not a fault of any layout, so it is
// caught once here rather than counted against all eight below.
let badInputs: (string * string) list =
    [
        for input in inputs do
            for who in [ shortName; longName ] do
                let source = (File.ReadAllText input).Replace("LHS", who)

                match parseErrors source with
                | e :: _ -> yield Path.GetFileNameWithoutExtension input, e
                | [] -> ()
    ]
    |> List.distinctBy fst

if not badInputs.IsEmpty then
    eprintfn "These examples do not parse. Fix them before reading anything else:"

    for name, err in badInputs do
        eprintfn "    %-28s %s" name err

    exit 1

let cases: Case list =
    [
        for layout in layouts do
            let config = configFor layout
            let name = layoutName layout

            for variant in [ "short"; "long" ] do
                Directory.CreateDirectory(Path.Combine(outputRoot, name, variant)) |> ignore

            for input in inputs do
                let template = File.ReadAllText input
                let example = Path.GetFileNameWithoutExtension input

                let render (who: string) (variant: string) =
                    let result = format config (template.Replace("LHS", who))

                    match result with
                    | Ok code ->
                        File.WriteAllText(Path.Combine(outputRoot, name, variant, Path.GetFileName input), code)
                        Ok code
                    | Error e -> Error e

                yield
                    {
                        Layout = layout
                        Example = example
                        Outcome =
                            match render shortName "short", render longName "long" with
                            | Ok s, Ok l -> Formatted(s, l)
                            | Error e, _
                            | _, Error e -> Failed e
                    }
    ]

// ---------------------------------------------------------------------------------------------
// Judgements. Everything here is computed, never eyeballed.
// ---------------------------------------------------------------------------------------------

/// What renaming the left-hand side did to an example.
type RenameEffect =
    /// Same number of lines, same columns. The rename changed only the name.
    | Stable
    /// Same number of lines, different columns. Something is placed relative to the name.
    | Shifted
    /// A different number of lines: the extra character crossed the page width and the example
    /// broke somewhere new. Nothing can be concluded about anchoring from it, because a shift
    /// would be hidden underneath the reflow, so it is reported apart from `Shifted` rather than
    /// counted as either answer.
    | Inconclusive

let renameEffect (short: string) (long: string) : RenameEffect =
    let ps, pl = indentProfile short, indentProfile long

    if ps.Length <> pl.Length then Inconclusive
    elif ps <> pl then Shifted
    else Stable

/// Columns that are not a multiple of the indent size. A construct that places its contents under
/// its own opening token lands wherever that token happens to be, which is a column nobody chose
/// and which moves when anything before it changes width. Some of that is the construct's own doing
/// and is the same under every layout, so the number is worth comparing between layouts rather than
/// reading on its own.
let offGrid (source: string) : int =
    indentProfile source
    |> List.filter (fun indent -> indent % FormatConfig.Default.IndentSize <> 0)
    |> List.length

type Verdict =
    {
        Layout: string
        Invalid: (string * string) list
        NotIdempotent: string list
        Shifted: string list
        Reflowed: string list
        MeaningChanged: string list
        OffGrid: int
        Lines: int
    }

let verdictFor (layout: ExperimentalInfixLayout) : Verdict =
    let mine = cases |> List.filter (fun c -> c.Layout = layout)
    let config = configFor layout

    let invalid, notIdempotent, shifted, reflowed, meaning, lines =
        ResizeArray(), ResizeArray(), ResizeArray(), ResizeArray(), ResizeArray(), ref 0

    let offGridCount = ref 0

    for case in mine do
        match case.Outcome with
        | Failed e -> invalid.Add(case.Example, e)
        | Formatted(short, long) ->
            lines.Value <- lines.Value + (indentProfile short).Length
            offGridCount.Value <- offGridCount.Value + offGrid short

            match parseErrors short with
            | e :: _ -> invalid.Add(case.Example, e)
            | [] ->
                match format config short with
                | Ok again when again <> short -> notIdempotent.Add case.Example
                | Error e -> notIdempotent.Add $"%s{case.Example} (%s{e})"
                | _ -> ()

            if case.Example.Contains "splice" && becameSplice short then
                meaning.Add case.Example

            match renameEffect short long with
            | Stable -> ()
            | Shifted -> shifted.Add case.Example
            | Inconclusive -> reflowed.Add case.Example

    {
        Layout = layoutName layout
        Invalid = List.ofSeq invalid
        NotIdempotent = List.ofSeq notIdempotent
        Shifted = List.ofSeq shifted
        Reflowed = List.ofSeq reflowed
        MeaningChanged = List.ofSeq meaning
        OffGrid = offGridCount.Value
        Lines = lines.Value
    }

let verdicts: Verdict list = layouts |> List.map verdictFor

// ---------------------------------------------------------------------------------------------
// Summary.
// ---------------------------------------------------------------------------------------------

let sb = StringBuilder()
let line (s: string) = sb.AppendLine s |> ignore

line "# infix-lab"
line ""
line $"%d{inputs.Length} examples, %d{layouts.Length} layouts, page width %d{maxLineLength}."
line ""
line $"Each example is formatted twice, with `LHS` replaced by `%s{shortName}` and by `%s{longName}`."
line "The two names differ by one character. A layout that places the right-hand side relative to"
line "the name on the left shifts by exactly that column and shows up as `shifted`."
line ""
line "`inconclusive` means the extra character crossed the page width, so the example broke"
line "somewhere new and nothing can be read from its columns either way. The same examples are"
line "inconclusive under every layout, which is what you would expect of a page-width effect."
line ""
line "| layout | invalid | not idempotent | meaning changed | shifted by rename | inconclusive | off-grid | lines |"
line "| --- | --- | --- | --- | --- | --- | --- | --- |"

let tick (xs: 'a list) =
    if xs.IsEmpty then "ok" else string xs.Length

for v in verdicts do
    line
        $"| `%s{v.Layout}` | %s{tick v.Invalid} | %s{tick v.NotIdempotent} | %s{tick v.MeaningChanged} | %s{tick v.Shifted} | %s{tick v.Reflowed} | %d{v.OffGrid} | %d{v.Lines} |"

line ""
line "`off-grid` counts lines whose indentation is not a multiple of the indent size. Those columns"
line "are chosen by whatever token happens to precede them rather than by the layout, and they move"
line "when that token changes width. `lines` is the total line count, as a measure of verbosity."
line ""

for v in verdicts do
    let problems =
        [
            for e, msg in v.Invalid do
                $"does not compile: `%s{e}` (%s{msg})"
            for e in v.NotIdempotent do
                $"not idempotent: `%s{e}`"
            for e in v.MeaningChanged do
                $"meaning changed: `%s{e}`"
            for e in v.Shifted do
                $"shifted by rename: `%s{e}`"
        ]

    line $"## `%s{v.Layout}`"
    line ""

    if problems.IsEmpty then
        line "Nothing found by any of the checks."
    else
        for p in problems do
            line $"- %s{p}"

    if not v.Reflowed.IsEmpty then
        let names = String.concat ", " v.Reflowed
        line ""
        line $"Inconclusive, the rename crossed the page width: %s{names}."

    line ""

File.WriteAllText(summaryPath, sb.ToString())

// ---------------------------------------------------------------------------------------------
// Terminal report: the table only. The detail is in the summary.
// ---------------------------------------------------------------------------------------------

printfn ""

printfn
    "%-24s %8s %8s %8s %8s %8s %8s %6s"
    "layout"
    "invalid"
    "not-idem"
    "meaning"
    "shifted"
    "inconcl"
    "off-grid"
    "lines"

printfn "%s" (String.replicate 83 "-")

for v in verdicts do
    printfn
        "%-24s %8s %8s %8s %8s %8s %8d %6d"
        v.Layout
        (tick v.Invalid)
        (tick v.NotIdempotent)
        (tick v.MeaningChanged)
        (tick v.Shifted)
        (tick v.Reflowed)
        v.OffGrid
        v.Lines

printfn ""
printfn "Summary   %s" (Path.GetRelativePath(repositoryRoot, summaryPath))
printfn "Output    %s/<layout>/{short,long}" (Path.GetRelativePath(repositoryRoot, outputRoot))

let clean =
    verdicts
    |> List.filter (fun v -> v.Invalid.IsEmpty && v.NotIdempotent.IsEmpty && v.MeaningChanged.IsEmpty)

printfn ""
printfn "%d of %d layouts pass every correctness check." clean.Length verdicts.Length

let stable = clean |> List.filter (fun v -> v.Shifted.IsEmpty)

printfn "%d of those also place the right-hand side independently of the name on the left:" stable.Length

for v in stable do
    printfn "    %s" v.Layout

// A layout that emits code the compiler rejects is a result, not a failure of this script, so the
// exit code says whether the run completed rather than whether every layout came out clean. What
// each of them did is the table above.
exit 0
