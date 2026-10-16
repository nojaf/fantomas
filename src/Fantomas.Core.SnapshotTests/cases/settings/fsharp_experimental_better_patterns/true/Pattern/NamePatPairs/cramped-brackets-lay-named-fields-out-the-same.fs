(*---
fsharp_experimental_better_patterns = true
fsharp_multiline_bracket_style = cramped
max_line_length = 60
---*)
match e with
| SynExpr.App(isInfix = true; funcExpr = SynExpr.LongIdent(longDotId = longDotId; extra = extra); argExpr = argExpr) -> body
| _ -> ()
