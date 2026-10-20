(*---
# With an indent of 2, what follows as goes one level further, so it stands right of where the
# pattern starts, after the bar. An as that is an item of a tuple there gets the same layout.
fsharp_experimental_better_patterns = true
indent_size = 2
max_line_length = 60
---*)
match e with
| SynExpr.App(ExprAtomicFlag.NonAtomic, false, funcExpr, argExpr, range) as application -> body
| SynExpr.Ident identifier as SynExpr.App(ExprAtomicFlag.NonAtomic, false, funcExpr, argExpr, range) -> body
| SynExpr.Ident someLongIdentifierName as someEvenLongerAliasForTheWholeThing -> body
| SynExpr.Ident x as y -> body
| SynExpr.Ident someLongIdentifierName as someEvenLongerAliasForTheWholeThing, otherArgument -> body
| _ -> ()
