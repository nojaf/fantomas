(*---
# With Stroustrup brackets a list at the top of a clause has nothing to hug, and its closing bracket
# cannot go back to the bar, so it looks the same as with aligned brackets.
fsharp_experimental_better_patterns = true
fsharp_multiline_bracket_style = stroustrup
max_line_length = 60
---*)
match e with
| [ SynBinding(headPat = SynPat.Named(ident = ident); expr = expr); SynBinding(headPat = SynPat.Named(ident = ident); expr = expr) ] -> body
| [| firstItemWithALongName; secondItemWithALongName; thirdItemWithALongName |] as items -> body
| [ a; b ] -> body
| [] -> ()
