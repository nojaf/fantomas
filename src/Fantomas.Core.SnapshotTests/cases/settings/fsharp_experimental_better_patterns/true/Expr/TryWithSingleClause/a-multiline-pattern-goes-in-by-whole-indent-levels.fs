(*---
# Without a bar before it, the only clause of a `with` steps its pattern in by whole indent levels,
# until a closer is no more than one column left of where the pattern starts. What is inside goes
# one level further in.
fsharp_experimental_better_patterns = true
max_line_length = 60
---*)
let namedFields () =
    try
        body
    with SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) ->
        handler

let tupleArgument () =
    try
        body
    with SynExpr.App(ExprAtomicFlag.NonAtomic, false, funcExpr, argExpr, range) ->
        handler

let parentheses () =
    try
        body
    with (SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr)) ->
        handler

let asPattern () =
    try
        body
    with SynExpr.Ident someLongIdentifierName as someEvenLongerAliasForTheWholeThing ->
        handler

let consChain () =
    try
        body
    with Some(firstLongIdentifier :: secondLongIdentifier :: thirdLongIdentifier) ->
        handler

let guard () =
    try
        body
    with SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) when isInfix ->
        handler

let short () =
    try
        body
    with SynExpr.Ident ident ->
        handler
