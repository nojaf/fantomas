(*---
fsharp_experimental_better_patterns = true
max_line_length = 60
---*)
match x with
| SomeCase(firstArgumentPattern = a; secondArgumentPattern = b)
| OtherCase(firstArgumentPattern = a; secondArgumentPattern = b) -> body
| _ -> ()
