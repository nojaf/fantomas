(*---
fsharp_experimental_better_patterns = true
indent_size = 2
max_line_length = 60
---*)
match e with
| Some(SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr)) -> body
| _ -> ()
