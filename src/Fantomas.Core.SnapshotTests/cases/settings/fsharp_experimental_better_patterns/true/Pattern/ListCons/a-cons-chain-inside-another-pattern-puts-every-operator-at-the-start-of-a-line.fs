(*---
# A :: chain inside another pattern that does not fit puts every :: at the start of a line, as the
# :: expression does.
fsharp_experimental_better_patterns = true
max_line_length = 60
---*)
match e with
| Some(SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) :: rest) -> body
| ParseArguments (SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) :: rest) otherArgument -> body
| SynModuleDecl.Let(bindings = SynBinding(headPat = SynPat.Named(ident = ident); expr = expr) :: rest) -> body
| [ SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) :: rest; otherList ] -> body
| _ -> ()
