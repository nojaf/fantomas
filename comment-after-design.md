# Comment-after closing bracket design

Branch: `comment-after-rebased`
Issue: 1233 and related

## Problem

When a comment is attached as `ContentAfter` on the last item inside an indented block (list `[]`, record `{}`), the emit order causes the closing bracket to land at the wrong indentation.

### Current emit order

```
genExpr lastItem
  → enterNode / leaveNode emits ContentAfter(comment)
  → WriteLineBecauseOfTrivia
→ UnIndentBy          ← indent level drops here
→ [lastWriteEventIsNewline? → true, so skip newline]
→ Write "]"           ← ends up at wrong indent
```

`lastWriteEventIsNewline` (Context.fs:340) skips `UnIndentBy` when scanning backwards, finds the trivia newline, returns `true`. The caller skips emitting a newline before `]`. But the indent level changed, so `]` stays on the comment's indentation instead of getting its own line.

### Example (list with Stroustrup style)

Input:
```fsharp
let list = [
    someItem
    // comment
]
```

Current output (wrong — `]` at column 4):
```fsharp
let list = [
    someItem
    // comment
    ]
```

Expected output (comment stays at content indent, `]` at column 0):
```fsharp
let list = [
    someItem
    // comment
]
```

### All 4 call sites of `lastWriteEventIsNewline` have this pattern

1. **CodePrinter.fs:1799** — record `}` after `indentSepNlnUnindent`
2. **CodePrinter.fs:1834** — aligned record `}` closing brace
3. **CodePrinter.fs:1904** — list/array `]` closing bracket (the failing test case)
4. **Context.fs:584** — `sepNlnUnlessLastEventIsNewline` general helper

## Approaches considered and rejected

### 1. Flip unindent before ContentAfter

Emit `unindent` before `leaveNode` so the comment prints at `]`'s indent level:

```
genExprAux lastItem → UnIndentBy → ContentAfter(comment) → WriteLineBecauseOfTrivia → "]"
```

**Rejected**: This shifts the comment from column 4 to column 0, changing its association from "after `someItem`" to "before `]`". The comment should stay at `someItem`'s indentation level for correct semantics and idempotency.

### 2. Fix `lastWriteEventIsNewline` to track pending unindents

Make `lastWriteEventIsNewline` return `false` when it crosses an `UnIndentBy` to find the newline.

**Rejected**: This only prevents the extra newline from being skipped, but we still need a newline at the *new* indent level. The `WriteLineBecauseOfTrivia` already produced a newline at the *old* indent level. Adding another newline would produce a blank line.

### 3. Unindent-aware `genTrivia` / flag on `CommentOnSingleLine`

Embed an "also unindent" flag in `CommentOnSingleLine` trivia content, or teach `genTrivia`/`leaveNode` about the caller's indentation needs.

**Rejected**: Mixes formatting concerns into the trivia data model. Breaks the clean separation between trivia content and formatting decisions.

## Chosen approach: capture and splice writer events

### Key insight

The comment should stay at `someItem`'s indentation (column 4). The `UnIndentBy` needs to be spliced into the event stream *between* the `WriteComment` and the final `WriteLineBecauseOfTrivia`. This way:
- The comment is emitted at the content's indent level (correct)
- The unindent takes effect before the trailing newline
- The trailing newline lands at the reduced indent level
- `]` follows naturally at column 0

### Desired event stream

```
Write "someItem"
WriteLineBecauseOfTrivia       ← newline before comment (at indent 4)
WriteComment "// comment"      ← comment at indent 4 ✓
UnIndentBy 4                   ← spliced in here
WriteLineBecauseOfTrivia       ← newline now at indent 0
Write "]"                      ← at column 0 ✓
```

### Implementation: `captureTrailingTriviaEvents`

A helper in CodePrinter.fs that runs `leaveNode` on a dummy context to capture the writer events it would produce, without applying them to the real context:

```fsharp
let captureTrailingTriviaEvents (node: Node) (currentCtx: Context) : WriterEvent list =
    let dummyCtx = { currentCtx with WriterModel = { currentCtx.WriterModel with Mode = Dummy }}
    let eventsBefore = dummyCtx.WriterEvents.Length
    let ctxAfter = leaveNode node dummyCtx
    let eventsAfter = ctxAfter.WriterEvents.Length
    let take = eventsAfter - eventsBefore
    ctxAfter.WriterEvents.Rev()
    |> Seq.take take
    |> Seq.toList
```

The caller then splices `UnIndentBy` before the last `WriteLineBecauseOfTrivia` and applies the modified events to the real context:

```fsharp
let updatedEvents =
    let rec visit (continuation: WriterEvent list -> WriterEvent list) next =
        match next with
        | [] -> continuation []
        | [ WriteComment c; WriteLineBecauseOfTrivia ] ->
            visit
                (fun current ->
                    WriteComment c
                    :: UnIndentBy ctx.Config.IndentSize
                    :: WriteLineBecauseOfTrivia
                    :: current
                    |> continuation)
                []
        | head :: rest -> visit (fun current -> (head :: current) |> continuation) rest
    visit id events

List.fold (fun acc event -> writerEvent event acc) ctx updatedEvents
```

### Using `colWithLast` for last-item handling

`colWithLast` (Context.fs) processes a list where the last item gets different treatment:

```fsharp
colWithLast
    genExpr          // normal items
    sepNln           // separator
    (fun lastExpr -> // last item: genExpr content + captured/spliced trivia events
        genExprContent lastExpr
        +> fun ctx ->
            let events = captureTrailingTriviaEvents (Expr.Node lastExpr) ctx
            let updatedEvents = spliceUnindent events ctx.Config.IndentSize
            List.fold (fun acc event -> writerEvent event acc) ctx updatedEvents)
    node.Elements
```

### Gating

Only use the special path when `HasContentAfterOfLastDescendant` is true on the container or last element. Normal path (no trailing trivia) stays unchanged.

## What's already done on this branch

- `ColMultilineItem` carries `Node` instead of pre-computed separator function (the `option` was removed since all callers pass a node)
- `HasContentAfterOfLastDescendant` + `MarkContentAfterOfLastDescendant` added to `Node` interface in SyntaxOak.fs
- Flag is set inline in `simpleTriviaToTriviaInstruction` (Trivia.fs) when `AddAfter` is called on a descendant
- `findNodeBeforeWithMatchingColumn` in Trivia.fs for column-matching comment assignment (indented comments attach to preceding node at same column)
- `colWithNlnWhenItemIsMultiline` computes `sepNlnItem` from `currentNode.HasContentBefore` instead of a pre-computed separator
- `addFinalNewline` in CodePrinter.fs handles trailing blank lines from deeply nested ContentAfter
- 4 new test cases in CommentTests.fs for issue 1233
- `lastDescendantHasContentAfter` removed from Context.fs (was dead code, replaced by the flag approach)
- `colWithLast` + `foldExceptLast` helpers added in Context.fs
- `captureTrailingTriviaEvents` helper added in CodePrinter.fs (prototype working for list case)
- Prototype produces correct output for the `comment before closing list bracket, 3079` test case

## Deeper problem: `unindent` and trailing trivia in `expressionExceedsPageWidth`

### Discovery

The `genLambdaAux` function (CodePrinter.fs:2047) has the same unindent-before-trivia problem, but it surfaces through a different path: `sepSpaceOrIndentAndNlnIfExpressionExceedsPageWidthUnlessStroustrup`.

Call chain:
1. `sepSpaceOrIndentAndNlnIfExpressionExceedsPageWidthUnlessStroustrup` (Context.fs:833)
2. → `sepSpaceOrIndentAndNlnIfExceedsPageWidthUnlessStroustrup` (Context.fs:827)
3. → `sepSpaceOrIndentAndNlnIfExpressionExceedsPageWidth` (Context.fs:789)
4. → `expressionExceedsPageWidth` (Context.fs:741) with `beforeLong = indent +> sepNln` and `afterLong = unindent`

Inside `expressionExceedsPageWidth`, the long-expression fallback is:

```fsharp
let fallbackExpression = beforeLong +> expr +> afterLong
```

Which expands to: `indent +> sepNln +> expr +> unindent`

The `unindent` runs *after* the expression, but trailing trivia (comments) attached to the last node inside `expr` may have already been flushed — or will be flushed after the unindent changes the indentation level.

### This is systemic

Every caller of `expressionExceedsPageWidth` that passes `unindent` as `afterLong` has this latent bug. The callers include:
- `autoIndentAndNlnIfExpressionExceedsPageWidth` (Context.fs:780)
- `sepSpaceOrIndentAndNlnIfExpressionExceedsPageWidth` (Context.fs:789)
- `sepSpaceOrDoubleIndentAndNlnIfExpressionExceedsPageWidth` (Context.fs:798)
- And transitively, `sepSpaceOrIndentAndNlnIfExpressionExceedsPageWidthUnlessStroustrup` (Context.fs:833)

The pattern should conceptually be:

```
indent +> sepNln +> expr +> [flush trailing trivia at current indent] +> unindent
```

But today it is:

```
indent +> sepNln +> expr +> unindent
```

### Why `unindent` timing isn't the issue

At first glance it looks like `UnIndentBy` fires too early. But actually, `IndentBy`/`UnIndentBy` only update `WriterModel.Indent` — the indent value is inert until the next `WriteLine`/`WriteLineBecauseOfTrivia`, which reads `m.Indent` to produce leading spaces (`String.replicate m.Indent " "` in `doNewline`). So the indent model itself is fine.

The real problem is the **interaction between trivia-emitted newlines and `lastWriteEventIsNewline`**:

1. `genExpr lastItem` runs → `leaveNode` emits `ContentAfter` trivia: `WriteComment "// comment"` + `WriteLineBecauseOfTrivia` (newline at current indent ✓)
2. `unindent` fires → updates `m.Indent` (no visible effect yet)
3. Caller checks `lastWriteEventIsNewline` → sees the trivia newline → returns `true` → **skips** emitting a newline before the closing bracket
4. Closing bracket writes on the same line as the trivia newline, but at the **old** indent level (the trivia newline used the pre-unindent indent)

If you force a newline anyway, you get a **blank line** (one from trivia, one forced). There's no good place to put `unindent` with the current emit order — before the trivia newline is wrong (comment at wrong indent), after is wrong (bracket at wrong indent), and adding an extra newline doubles up.

### The fix pattern (from the array/list case)

The last commit (`4279cbd`) solved this for `genArrayOrList` using `captureTrailingTriviaEvents` + `insertUnindent`:

1. **Release** `ContentAfter` from the last node before `genExpr` runs (`node.ReleaseContentAfter()`)
2. **Run** `genExpr` — which now skips the trivia since it's been released
3. **Capture** the trivia events on a dummy context (`captureTrailingTriviaEvents`)
4. **Splice** `UnIndentBy` between the comment and its trailing newline (`insertUnindent`)
5. **Replay** the modified events onto the real context

This gives exactly one newline at the correct (reduced) indent level.

### The `expressionExceedsPageWidth` problem

The same pattern needs to happen inside `expressionExceedsPageWidth` (Context.fs:741) and its 45 call sites in CodePrinter.fs. The long-expression fallback is:

```fsharp
let fallbackExpression = beforeLong +> expr +> afterLong
```

Where `afterLong = unindent`. The `unindent` needs to be spliced into the trivia events, not run after them. But `expressionExceedsPageWidth` doesn't have a handle on the node whose trivia needs releasing — it only receives an opaque `expr: Context -> Context` function.

### Implications

Weaving the fix into `expressionExceedsPageWidth` itself would fix all 45 call sites at once, but the abstraction doesn't have enough information: it doesn't know which node to call `ReleaseContentAfter()` on. The `expr` function has already closed over the node.

Possible directions:
- **Option A**: Change `expr` to cooperate — e.g., `expr` releases its own trivia and returns captured events alongside the context, so `expressionExceedsPageWidth` can splice before applying `afterLong`
- **Option B**: Give `expressionExceedsPageWidth` a node parameter so it can do the release/capture/splice itself
- **Option C**: Accept the whack-a-mole approach — fix each call site individually, using the same release/capture/splice pattern as `genArrayOrList`
- **Option D**: Rethink more fundamentally — e.g., make `unindent` trivia-aware so it defers past pending trivia events, or change how `leaveNode` emits trailing trivia so the caller retains control

Option A or B would require changing the signature of `expressionExceedsPageWidth` and all its wrappers. Option C is pragmatic but fragile (45 potential sites). Option D is the cleanest but the biggest change.

## Still TODO

- Replace hardcoded `!-"TODO"` / `!-"someItem"` with real `genExpr` / `genExprAux` (a `genExpr` variant without `genNode` wrapper)
- The splice `visit` pattern only handles `CommentOnSingleLine` — needs to cover `BlockComment`, `Directive` too
- Gate on `HasContentAfterOfLastDescendant` so normal path is unchanged
- Generalize across the other 3 `lastWriteEventIsNewline` call sites (records `{}`, etc.)
- Triage the 16 failing tests: which are regressions vs pre-existing
- Clean up temporary debug code (hardcoded strings, user's `+` experiment)

## Notes

- The `shared.fsx` editorconfig parser was fixed: commas → newlines, glob covers `*.{fs,fsx,fsi}`
- Writer events script (`scripts/writer-events.fsx`) and Oak script (`scripts/oak.fsx`) are useful for diagnosing these issues
