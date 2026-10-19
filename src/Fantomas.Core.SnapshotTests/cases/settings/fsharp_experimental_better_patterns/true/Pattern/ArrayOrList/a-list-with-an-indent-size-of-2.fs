(*---
# With an indent of 2, the items of a list inside another pattern go two columns in from where the
# list starts, and its closing bracket goes back there.
fsharp_experimental_better_patterns = true
indent_size = 2
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
