(*---
# As the value of a named argument does. There it gets a line of its own first, and only breaks
# when it does not fit on that line either.
fsharp_experimental_better_patterns = true
max_line_length = 60
---*)
match e with
| SynExpr.App(isInfix = true; funcExpr = SynExpr.LongIdent(longDotId = longDotIdentifier1); argExpr = argExpr) -> body
| SynExpr.App(isInfix = true; funcExpr = SynExpr.LongIdent(longDotId = longDotId; extra = extra); argExpr = argExpr) -> body
| _ -> ()
