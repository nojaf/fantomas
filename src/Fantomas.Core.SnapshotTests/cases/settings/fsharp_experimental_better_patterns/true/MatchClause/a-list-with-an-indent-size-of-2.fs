(*---
# With an indent of 2, the items of a list go a level further in, so they stand right of the closing
# bracket.
fsharp_experimental_better_patterns = true
indent_size = 2
max_line_length = 60
---*)
match e with
| [ SynBinding(headPat = SynPat.Named(ident = ident); expr = expr); SynBinding(headPat = SynPat.Named(ident = ident); expr = expr) ] -> body
| [| firstItemWithALongName; secondItemWithALongName; thirdItemWithALongName |] as items -> body
| [ a; b ] -> body
| [] -> ()
