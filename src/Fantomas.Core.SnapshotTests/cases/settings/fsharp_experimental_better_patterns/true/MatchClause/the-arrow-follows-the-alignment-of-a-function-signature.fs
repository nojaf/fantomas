(*---
# With `fsharp_align_function_signature_to_indentation`, the arrow after a multiline pattern gets a
# line of its own on the column of the body, as the `=` of a binding with a multiline signature does.
# After a pattern on one line it stays put.
fsharp_experimental_better_patterns = true
fsharp_align_function_signature_to_indentation = true
max_line_length = 60
---*)
match e with
| SynExpr.App(isInfix = true; argExprWithALongName = argExpr) -> body
| SomeVeryLongUnionCaseNameNumberOne | SomeVeryLongUnionCaseNameNumberTwo -> body
| _ -> ()
