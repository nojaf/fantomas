(*---
# As the argument of an application does. A pattern in parentheses on its own keeps hugging them,
# as an expression in parentheses does.
fsharp_experimental_better_patterns = true
max_line_length = 60
---*)
match ast with
| Some(ParsedImplFileInput(modules = modules; isScript = isScript; fileName = fileName)) -> body
| Some(Some(ParsedImplFileInput(modules = modules; isScript = isScript; fileName = fileName))) -> body
| (ParsedImplFileInput(modules = modules; isScript = isScript; fileName = fileName)) -> body
| _ -> ()
