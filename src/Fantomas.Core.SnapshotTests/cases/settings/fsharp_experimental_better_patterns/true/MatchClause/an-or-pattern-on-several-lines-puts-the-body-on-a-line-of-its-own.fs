(*---
# At the top of a clause an or-pattern always takes a line per alternative, however short. Nested
# in parentheses it stays on one line, and so does the body after it.
fsharp_experimental_better_patterns = true
---*)
match x with
| A | B -> body
| Some(A | B) -> body
| _ -> ()
