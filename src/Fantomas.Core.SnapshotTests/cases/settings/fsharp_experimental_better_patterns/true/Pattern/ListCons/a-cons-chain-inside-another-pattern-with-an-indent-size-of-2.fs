(*---
# With an indent of 2, a :: chain inside another pattern lays out its items the same way.
fsharp_experimental_better_patterns = true
indent_size = 2
max_line_length = 60
---*)
match e with
| Some(SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) :: rest) -> body
| ParseArguments (SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) :: rest) otherArgument -> body
| SynModuleDecl.Let(bindings = SynBinding(headPat = SynPat.Named(ident = ident); expr = expr) :: rest) -> body
| [ SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) :: rest; otherList ] -> body
| _ -> ()
