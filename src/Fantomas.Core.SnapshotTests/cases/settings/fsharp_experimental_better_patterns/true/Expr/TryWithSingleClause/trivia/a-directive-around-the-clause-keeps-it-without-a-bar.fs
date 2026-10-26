(*---
# A directive around the only clause of a `with` puts each pattern on a line of its own, without a
# bar.
fsharp_experimental_better_patterns = true
max_line_length = 60
---*)
let f () =
    try
        body
    with
#if DEBUG
    SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) ->
        handler
#else
    _ -> fallback
#endif
