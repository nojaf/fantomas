# Writer events rethink: mutable doubly-linked list + deferred materialization

## Motivation

The current architecture has two pain points:

1. **The trivia-before-unindent problem**: `expressionExceedsPageWidth` constructs `beforeLong +> expr +> afterLong` where `afterLong = unindent`. Trailing trivia (comments) emitted inside `expr` by `leaveNode` interact badly with the `unindent` — there's no clean place to splice `UnIndentBy` into the event stream. The current fix (`captureTrailingTriviaEvents`) requires releasing trivia from nodes, running a dummy context, capturing events, splicing, and replaying. This needs to happen at each of 45+ call sites.

2. **ShortExpression double-execution**: `expressionExceedsPageWidth` runs `expr` once in `ShortExpression` mode, checks if it fits, and if not, runs `expr` again in normal mode. The `colWithNlnWhenItemIsMultiline` pattern similarly needs to know if items are multiline before deciding separators.

Both problems share a root cause: the writer event stream is **append-only and immutable**, so you can't go back and modify what was already emitted.

See `comment-after-design.md` for the concrete example that motivated this rethink (trailing comments before closing `]`/`}` brackets).

## Core idea

Replace the immutable `Queue<WriterEvent>` with a **mutable doubly-linked list** (DLL). Defer string materialization to the end. During CodePrinter, only track lightweight metadata (line count, column, indent) for formatting decisions.

## Resolved design decisions

### Collection type: custom DLL

A custom DLL rather than `System.Collections.Generic.LinkedList<T>`. The key operation that `LinkedList<T>` cannot support is O(1) truncation (restore): its node links are encapsulated, so truncation requires O(k) `RemoveLast()` calls. For a large expression that doesn't fit on one line, `k` could be thousands of events.

The custom DLL is ~50 lines and does exactly what we need. No `Count` field — nothing uses it. The two "cursor" use cases (scan new events, restore to snapshot) both work via saved node references.

### The DLL node

```fsharp
[<AllowNullLiteral>]
type EventNode(event: WriterEvent) =
    member val Event = event with get, set
    member val Prev: EventNode = null with get, set
    member val Next: EventNode = null with get, set
```

### The event list

```fsharp
type EventList() =
    member val Head: EventNode = null with get, set
    member val Tail: EventNode = null with get, set

    /// O(1) append — returns the node for future reference
    member this.Append(event: WriterEvent) =
        let node = EventNode(event)
        if isNull this.Tail then
            this.Head <- node
            this.Tail <- node
        else
            node.Prev <- this.Tail
            this.Tail.Next <- node
            this.Tail <- node
        node

    /// O(1) insert after a given node
    member this.InsertAfter(after: EventNode, event: WriterEvent) =
        let node = EventNode(event)
        node.Prev <- after
        node.Next <- after.Next
        if not (isNull after.Next) then
            after.Next.Prev <- node
        else
            this.Tail <- node
        after.Next <- node
        node

    /// O(1) insert before a given node
    member this.InsertBefore(before: EventNode, event: WriterEvent) =
        let node = EventNode(event)
        node.Next <- before
        node.Prev <- before.Prev
        if not (isNull before.Prev) then
            before.Prev.Next <- node
        else
            this.Head <- node
        before.Prev <- node
        node

    /// O(1) remove
    member this.Remove(node: EventNode) =
        if not (isNull node.Prev) then node.Prev.Next <- node.Next else this.Head <- node.Next
        if not (isNull node.Next) then node.Next.Prev <- node.Prev else this.Tail <- node.Prev

    /// O(1) snapshot — save a reference to the current tail
    member this.Snapshot() : EventNode = this.Tail

    /// O(1) restore — truncate everything after the snapshot point
    member this.Restore(snapshot: EventNode) =
        if isNull snapshot then
            this.Head <- null
            this.Tail <- null
        else
            snapshot.Next <- null
            this.Tail <- snapshot
```

### Single shared mutable DLL on Context

`Context` holds one `EventList` instance. All formatting operations append to the same DLL. This replaces the current `WriterEvents: Queue<WriterEvent>`.

Because the DLL is shared and mutable, speculative execution (short-expression checks, dummy probes) **must** use snapshot/restore rather than re-running on a separate context. A missed restore corrupts the event stream.

### Snapshot/restore

Two use cases, both via saved node references:

1. **Non-destructive scan** (cursor): save tail reference, run `expr`, walk from `savedNode.Next` forward to inspect new events. Keep events. Used by `isMultilineItem`.

2. **Destructive restore** (rewind): save tail reference + full `WriterModel`, run `expr`, check result, either keep or truncate DLL back to saved node and restore `WriterModel`. Used by `expressionExceedsPageWidth` and `futureNlnCheck`/`exceedsWidth` (dummy probes).

### Nested snapshots

Nested speculative execution (e.g. outer `expressionExceedsPageWidth` contains inner `expressionExceedsPageWidth`) works naturally: snapshots are ordered along the DLL. Restoring an outer snapshot implicitly discards everything the inner level did. The call stack enforces LIFO ordering.

A debug assertion should verify that the snapshot node is still reachable from the tail on restore — this catches misuse where a child snapshot is used after its parent was restored.

### Snapshot includes full WriterModel

```fsharp
type Snapshot = { Node: EventNode; Model: WriterModel }
```

On restore: truncate DLL to `Node`, reset `WriterModel` to `Model`. The `Mode` field (e.g. `ShortExpression`) is handled explicitly by the caller after restore, same as today.

### Dummy mode uses the same DLL

`futureNlnCheck` and `exceedsWidth` currently create a throwaway context with `Queue.empty`. With the DLL, they instead snapshot, run the probe, read the answer from `WriterModel`, and restore. No separate collection needed. `WithDummy` simplifies to setting `Mode = Dummy` (and `MaxLineLength = Int32.MaxValue`) without swapping the event collection.

### ShortExpression mode stays

`ShortExpression` mode provides early-exit optimization: once `ConfirmedMultiline` is set, `WriterModel.update` stops updating the model, and nested `expressionExceedsPageWidth` calls short-circuit entirely. This remains valuable with the DLL — events still append (and get discarded on restore), but the metadata freeze prevents unnecessary work in nested calls.

## WriterModel during processing

Replace `Lines: string list` with `LineCount: int`. The `Lines` content is only inspected in two places during formatting (`genTrivia` and `addFinalNewline`), and both can derive what they need by walking backward from the DLL tail to the nearest newline event.

```fsharp
{ LineCount: int
  Column: int
  Indent: int
  AtColumn: int
  WriteBeforeNewline: string
  Mode: WriterModelMode }
```

`WriteBeforeNewline` stays as a model field for now — changing it isn't needed to solve the core problems.

### Two-phase processing

1. **Metadata update** (runs during formatting): `WriterModel.update` processes each event as it's appended, updating `LineCount`, `Column`, `Indent`, `AtColumn`, `WriteBeforeNewline`, and `ShortExpression` mode state. No string building.

2. **String materialization** (runs once at the end): `dump` walks the DLL head-to-tail, building output strings. This is essentially a second pass that only cares about producing the final text.

### Current line content derived on demand

When `genTrivia` needs to check "does the current line have content?" or "is the last character a space?", walk backward from the DLL tail to the nearest newline event, collecting `Write`/`WriteComment` texts. This replaces `List.tryHead` on `Lines`. The walk is bounded by the number of events on the current line — typically a handful.

## How this solves the trivia-before-unindent problem

Instead of the release/capture/replay dance:

1. `genExpr lastItem` runs normally — `leaveNode` emits comment + `WriteLineBecauseOfTrivia` into the DLL
2. Walk backward from `events.Tail` to find the trivia boundary (`WriteComment` + `WriteLineBecauseOfTrivia` pattern)
3. Insert `UnIndentBy` between the comment and the trailing newline — O(1) in-place splice
4. No dummy context, no `ReleaseContentAfter`, no `captureTrailingTriviaEvents`

### `expressionExceedsPageWidth` becomes unindent-aware

Replace the 4 opaque `Context -> Context` parameters with structured data:

```fsharp
type LongExpressionLayout =
    | IndentAndUnindent          // indent +> sepNln ... unindent
    | DoubleIndentAndUnindent    // indent +> indent +> sepNln ... unindent +> unindent
    | NewlineOnly                // sepNln ... (no unindent)
```

The function owns the splice logic: after `expr` runs on the long path, if the layout involves unindent, walk backward from DLL tail, find the trivia boundary, splice `UnIndentBy` before the trailing newline. This fixes all ~43 call sites at once.

The `beforeShort`/`afterShort` parameters stay (they're just `sepSpace`/`sepNone` variations). Stroustrup variants bypass `expressionExceedsPageWidth` entirely and are unaffected.

## How this helps `colWithNlnWhenItemIsMultiline`

Two new `WriterEvent` cases:

```fsharp
| Start       // marks the beginning of a colWithNlnWhenItemIsMultiline block
| Placeholder // placeholder separator between items, resolved after all items are emitted
```

Both are no-ops in `WriterModel.update`.

Flow:

1. Emit `Start` marker, save its DLL node reference
2. For each item, emit a `Placeholder` marker (save reference), then run the item's `expr`
3. After all items are emitted, walk backward from DLL tail to the `Start` marker
4. For each `Placeholder`, check the events between it and the next placeholder (or tail) to determine if that item was multiline
5. Replace each `Placeholder` node's event with the appropriate separator (`sepNln` or `sepNln + sepNln`)
6. Remove the `Start` marker and all resolved `Placeholder` markers

No re-execution. No dummy contexts. Items are formatted once.

## Migration approach

Big bang on a branch:

1. Introduce `EventList` type (new file, e.g. `EventList.fs`)
2. Add `Start` and `Placeholder` cases to `WriterEvent`
3. Change `Context.WriterEvents` from `Queue<WriterEvent>` to `EventList`
4. Update `WriterModel` — replace `Lines: string list` with `LineCount: int`
5. Make everything compile with `failwith` stubs where needed
6. Fix tests starting from `CodePrinterHelperFunctionsTests.fs` (simplest, illustrates core patterns), then work up to the full formatter suite
7. Refactor `expressionExceedsPageWidth` to use `LongExpressionLayout` DU
8. Refactor `colWithNlnWhenItemIsMultiline` to use `Start`/`Placeholder` markers

The test suite is the validation — if all tests pass, we're done.
