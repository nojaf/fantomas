(*---
# With Stroustrup brackets a list that is the single argument of a name or the value of a named field
# opens on that line: `Some [`, `Some([` and `bindings = [`. Its closing bracket goes back to where the
# pattern or the field starts. Anywhere else the list looks the same as aligned.
fsharp_experimental_better_patterns = true
fsharp_multiline_bracket_style = stroustrup
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
