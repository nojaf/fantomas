(*---
# With cramped brackets a list at the top of a clause keeps its items behind the opening bracket, as
# it did before, and a closer inside an item goes under the start of that item. A list of one
# or-pattern puts each alternative on a line of its own, its bar one column right of the item.
fsharp_experimental_better_patterns = true
fsharp_multiline_bracket_style = cramped
max_line_length = 60
---*)
match e with
| [ SynBinding(headPat = SynPat.Named(ident = ident); expr = expr); SynBinding(headPat = SynPat.Named(ident = ident); expr = expr) ] -> body
| [| firstItemWithALongName; secondItemWithALongName; thirdItemWithALongName |] as items -> body
| [ SynExpr.Ident firstIdentifier | SynExpr.Ident secondIdentifier | SynExpr.Ident thirdIdentifier ] -> body
| [ a; b ] -> body
| [] -> ()
