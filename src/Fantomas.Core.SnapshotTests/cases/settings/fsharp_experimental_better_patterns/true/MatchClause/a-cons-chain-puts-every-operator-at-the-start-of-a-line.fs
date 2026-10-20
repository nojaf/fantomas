(*---
# A :: chain at the top of a clause that does not fit puts every :: at the start of a line, two
# columns right of the bar, as the :: expression does. Each item is laid out as a pattern there.
# Parentheses around the chain go on lines of their own.
fsharp_experimental_better_patterns = true
max_line_length = 60
---*)
match e with
| x :: rest -> body
| firstIdentifier :: secondIdentifier :: thirdIdentifier :: rest -> body
| SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) :: rest -> body
| head :: SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) -> body
| SynExpr.App(ExprAtomicFlag.NonAtomic, false, funcExpr, argExpr, range) :: rest -> body
| head :: SynExpr.App(ExprAtomicFlag.NonAtomic, false, funcExpr, argExpr, range) -> body
| SynExpr.App(isInfix = true; argExpr = argExpr) :: SynExpr.Lambda(fromMethod = fromMethod; body = lambdaBody) :: rest -> body
| [ SynExpr.Ident firstIdentifier; SynExpr.Ident secondIdentifier ] :: rest -> body
| SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) :: rest, otherArgument -> body
| (SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) :: rest) -> body
| _ -> ()
