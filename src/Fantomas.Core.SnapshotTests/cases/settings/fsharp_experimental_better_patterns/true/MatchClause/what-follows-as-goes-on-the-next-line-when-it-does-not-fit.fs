(*---
# At the top of a clause, what follows as goes on the next line, one indent level in from the bar,
# when it does not fit behind as: a long alias, or a pattern that goes over several lines. A short
# one stays. An as that is an item of a tuple there gets the same layout.
fsharp_experimental_better_patterns = true
max_line_length = 60
---*)
match e with
| SynExpr.App(ExprAtomicFlag.NonAtomic, false, funcExpr, argExpr, range) as application -> body
| SynExpr.Ident identifier as SynExpr.App(ExprAtomicFlag.NonAtomic, false, funcExpr, argExpr, range) -> body
| SynExpr.Ident someLongIdentifierName as someEvenLongerAliasForTheWholeThing -> body
| SynExpr.Ident x as y -> body
| SynExpr.Ident someLongIdentifierName as someEvenLongerAliasForTheWholeThing, otherArgument -> body
| _ -> ()
