(*---
fsharp_experimental_better_patterns = true
max_line_length = 60
---*)
let (SomeCase(firstArgumentPattern = a; secondArgumentPattern = b)) = x

let f =
    fun (SomeCase(firstArgumentPattern = a; secondArgumentPattern = b)) -> a
