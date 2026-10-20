(*---
# A tuple at the top of a clause that does not fit puts one item per line, where the first one
# starts. Curried arguments that do not fit go on lines of their own, one indent level in, as the
# arguments of an application do.
fsharp_experimental_better_patterns = true
max_line_length = 60
---*)
match e with
| SynExpr.Ident firstIdentifier, SynExpr.Ident secondIdentifier, SynExpr.Ident thirdIdentifier -> body
| SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr), SynExpr.Ident secondIdentifier -> body
| ParseArguments firstArgumentPattern secondArgumentPattern thirdArgumentPattern -> body
| a, b -> body
| _ -> ()
