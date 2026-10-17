(*---
# A tuple in parentheses that does not fit puts one item per line, as the arguments of a method call
# do, and its closing parenthesis goes on a line of its own.
fsharp_experimental_better_patterns = true
max_line_length = 60
---*)
match e with
| SynExpr.App(ExprAtomicFlag.NonAtomic, false, SynExpr.Ident funcIdent, argumentExpression, range) -> body
| GetSetMember(Some(SynBinding(accessibility = getterAccessibility)), Some(setterBinding)) -> body
| Some(a, b) -> body
| _ -> ()
