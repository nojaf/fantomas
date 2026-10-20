(*---
# Inside another pattern, what follows as goes on the next line, one indent level in, when it does
# not fit behind as: a long alias, or a pattern that goes over several lines. A short one stays.
fsharp_experimental_better_patterns = true
max_line_length = 60
---*)
match e with
| Some(SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) as app) -> body
| Some(SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) as someVeryLongAliasThatDoesNotFitBehindTheCloserOfThePattern) -> body
| Some(SynExpr.Ident identifier as SynExpr.App(ExprAtomicFlag.NonAtomic, false, funcExpr, argExpr, range)) -> body
| Some(SynExpr.Ident someLongIdentifierName as someEvenLongerAliasForTheWholeThing) -> body
| _ -> ()
