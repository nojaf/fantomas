(*---
# With an indent of 2, curried arguments go a level further in, so they stand right of the name.
fsharp_experimental_better_patterns = true
indent_size = 2
max_line_length = 60
---*)
match e with
| ParseArguments firstArgumentPattern secondArgumentPattern thirdArgumentPattern -> body
| _ -> ()
