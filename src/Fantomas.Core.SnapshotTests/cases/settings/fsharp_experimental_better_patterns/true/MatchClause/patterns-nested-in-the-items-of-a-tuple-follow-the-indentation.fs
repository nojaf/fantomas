(*---
# Each item of a tuple at the top of a clause starts after `| `, and its own closer goes back there.
# What is nested inside an item follows the indentation of the file, as anywhere else.
fsharp_experimental_better_patterns = true
max_line_length = 60
---*)
match e with
| SynExpr.App(ExprAtomicFlag.NonAtomic, false, SynExpr.Ident funcIdent, argumentExpression, range), Some(SynBinding(accessibility = getterAccessibility; attributes = attributes)) -> body
| Some(SynExpr.App(ExprAtomicFlag.NonAtomic, false, SynExpr.Ident funcIdent, argumentExpression, range)), ParseArguments firstArgumentPattern secondArgumentPattern thirdArgumentPattern -> body
| ParseArguments (Some(SynExpr.App(ExprAtomicFlag.NonAtomic, false, funcExpr, argExpr, range))) secondArgumentPattern, other -> body
| _ -> ()
