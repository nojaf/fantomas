(*---
# Inside another pattern, parentheses stay next to the pattern inside them, (Some( and )), as they do
# around an expression, and their closer goes back to the start of its line.
fsharp_experimental_better_patterns = true
max_line_length = 60
---*)
match e with
| ParseArguments (Some(SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr))) otherArgument -> body
| ParseArguments (SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) as app) otherArgument -> body
| Some((SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) as app), otherArgument) -> body
| SynModuleDecl.Let(bindings = (SynBinding(headPat = SynPat.Named(ident = ident); expr = expr) as binding) :: rest) -> body
| [ (SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) as app); otherItem ] -> body
| _ -> ()
