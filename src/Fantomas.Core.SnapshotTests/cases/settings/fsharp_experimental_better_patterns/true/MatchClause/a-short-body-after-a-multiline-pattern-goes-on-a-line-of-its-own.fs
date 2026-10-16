(*---
# As the body of a binding does after a multiline signature.
fsharp_experimental_better_patterns = true
max_line_length = 60
---*)
match e with
| SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) -> body
| _ -> ()
