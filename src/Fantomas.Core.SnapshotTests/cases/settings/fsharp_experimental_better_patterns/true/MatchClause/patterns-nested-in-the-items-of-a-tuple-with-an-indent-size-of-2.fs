(*---
# With an indent of 2, the first level inside a tuple item goes a level further, right of its closer.
# What is nested deeper follows the indentation of the file, two columns per level.
fsharp_experimental_better_patterns = true
indent_size = 2
max_line_length = 60
---*)
match e with
| SynExpr.App(ExprAtomicFlag.NonAtomic, false, SynExpr.Ident funcIdent, argumentExpression, range), Some(SynBinding(accessibility = getterAccessibility; attributes = attributes)) -> body
| Some(SynExpr.App(ExprAtomicFlag.NonAtomic, false, SynExpr.Ident funcIdent, argumentExpression, range)), ParseArguments firstArgumentPattern secondArgumentPattern thirdArgumentPattern -> body
| ParseArguments (Some(SynExpr.App(ExprAtomicFlag.NonAtomic, false, funcExpr, argExpr, range))) secondArgumentPattern, other -> body
| _ -> ()
