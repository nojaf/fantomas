(*---
# The colon of a type annotation behind the closer follows fsharp_space_before_colon.
fsharp_experimental_better_patterns = true
fsharp_space_before_colon = true
max_line_length = 60
---*)
match e with
| (SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr): SynExpr) -> body
| (SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr): SynExpr), otherArgument -> body
| _ -> ()
