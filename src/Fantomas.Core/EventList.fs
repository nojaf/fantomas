namespace Fantomas.Core

/// A node in the mutable doubly-linked list of WriterEvents.
/// We use [<AllowNullLiteral>] instead of option for Prev/Next links because this is a hot path —
/// every formatting operation appends nodes. Option would allocate a Some wrapper on every link assignment,
/// adding GC pressure for no functional benefit. The null checks are contained within EventList's methods;
/// callers work with non-null EventNode references returned by Append/InsertAfter/InsertBefore.
[<AllowNullLiteral>]
type EventNode(event: WriterEvent) =
    member val Event = event with get, set
    member val Prev: EventNode = null with get, set
    member val Next: EventNode = null with get, set

/// Mutable doubly-linked list of WriterEvents.
/// Supports O(1) append, insert, remove, and truncation.
type EventList() =
    member val Head: EventNode = null with get, set
    member val Tail: EventNode = null with get, set

    /// O(1) append — returns the node for future reference.
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

    /// O(1) insert after a given node — returns the new node.
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

    /// O(1) insert before a given node — returns the new node.
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

    /// O(1) remove a node from the list.
    member this.Remove(node: EventNode) =
        if not (isNull node.Prev) then
            node.Prev.Next <- node.Next
        else
            this.Head <- node.Next

        if not (isNull node.Next) then
            node.Next.Prev <- node.Prev
        else
            this.Tail <- node.Prev

        node.Prev <- null
        node.Next <- null

    /// O(1) — mark the current end of the list so we can later discard everything appended after it.
    /// Used by speculative formatting: create a backup point, try an expression, and RollbackTo if it doesn't fit.
    member this.CreateBackupPoint() : EventNode = this.Tail

    /// O(1) — discard every node appended after `point`, restoring the list to where it was when CreateBackupPoint was called.
    /// Pass null to clear the entire list (when the backup point was created on an empty list).
    member this.RollbackTo(point: EventNode) =
        if isNull point then
            this.Head <- null
            this.Tail <- null
        else
            let discarded = point.Next

            if not (isNull discarded) then
                discarded.Prev <- null

            point.Next <- null
            this.Tail <- point

    /// Collect the text content of the current (last) line by walking backward from the tail
    /// to the nearest newline event. Returns the concatenated Write texts in forward order.
    member this.CurrentLineContent() =
        let writes = ResizeArray<string>()
        let mutable current = this.Tail

        while not (isNull current) do
            match current.Event with
            | WriteLine
            | WriteLineBecauseOfTrivia
            | WriteLineInsideStringConst -> current <- null // stop
            | Write w
            | WriteTrivia w ->
                writes.Add(w)
                current <- current.Prev
            | _ -> current <- current.Prev

        writes.Reverse()
        System.String.Concat(writes)

    /// Iterate events from head to tail.
    member this.ToSeq() =
        seq {
            let mutable current = this.Head

            while not (isNull current) do
                yield current.Event
                current <- current.Next
        }

    /// Iterate events from tail to head (reverse order).
    member this.ToRevSeq() =
        seq {
            let mutable current = this.Tail

            while not (isNull current) do
                yield current.Event
                current <- current.Prev
        }
