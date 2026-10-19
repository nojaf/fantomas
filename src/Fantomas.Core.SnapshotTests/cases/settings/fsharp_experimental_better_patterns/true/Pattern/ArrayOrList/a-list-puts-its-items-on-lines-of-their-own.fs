(*---
# A list inside another pattern that does not fit puts its items on lines of their own, one indent
# level in from where the list starts, as a list expression does. Its closing bracket goes back there.
fsharp_experimental_better_patterns = true
max_line_length = 60
---*)
match e with
| Some([ SynExpr.Ident firstIdentifier; SynExpr.Ident secondIdentifier; SynExpr.Ident thirdIdentifier ]) -> body
| Some [ SynExpr.Ident firstIdentifier; SynExpr.Ident secondIdentifier; SynExpr.Ident thirdIdentifier ] -> body
| SynExpr.Tuple(false, [ SynExpr.Ident firstIdentifier; SynExpr.Ident secondIdentifier ], commaRanges, range) -> body
| ParseArguments [ SynExpr.Ident firstIdentifier; SynExpr.Ident secondIdentifier ] otherArgument -> body
| [ [ SynExpr.Ident firstIdentifier; SynExpr.Ident secondIdentifier ]; [ SynExpr.Ident thirdIdentifier ] ] -> body
| SynModuleDecl.Let(bindings = [ SynBinding(headPat = SynPat.Named(ident = ident); expr = expr) as binding ]) -> body
| Some [| a; b |] -> body
| _ -> ()
