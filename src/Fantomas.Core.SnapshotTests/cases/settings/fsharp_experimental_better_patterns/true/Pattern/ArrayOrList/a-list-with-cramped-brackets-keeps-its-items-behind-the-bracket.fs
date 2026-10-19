(*---
# With cramped brackets a list inside another pattern keeps its items behind the opening bracket, as
# it did before.
fsharp_experimental_better_patterns = true
fsharp_multiline_bracket_style = cramped
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
