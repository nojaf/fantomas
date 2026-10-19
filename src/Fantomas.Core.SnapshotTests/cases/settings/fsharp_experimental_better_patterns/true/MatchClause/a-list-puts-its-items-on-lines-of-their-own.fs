(*---
# A list at the top of a clause that does not fit puts its items on lines of their own, one indent
# level in, as a list expression does. Its closing bracket goes back to where the list starts.
fsharp_experimental_better_patterns = true
max_line_length = 60
---*)
match e with
| [ SynBinding(headPat = SynPat.Named(ident = ident); expr = expr); SynBinding(headPat = SynPat.Named(ident = ident); expr = expr) ] -> body
| [| firstItemWithALongName; secondItemWithALongName; thirdItemWithALongName |] as items -> body
| [ a; b ] -> body
| [] -> ()
