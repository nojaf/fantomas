(*---
# A pattern in parentheses at the top of a clause that does not fit puts each parenthesis on a line
# of its own: the closer two columns right of the bar, what is inside one indent level in from the
# bar. A type annotation and an as stay behind the closer of what is inside, and what follows them
# goes on the next line, one indent level in, when it does not fit. Parentheses as an item of a
# tuple there get the same layout. Short parentheses stay on the line.
fsharp_experimental_better_patterns = true
max_line_length = 60
---*)
match e with
| (SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr)) -> body
| (SynExpr.App(ExprAtomicFlag.NonAtomic, false, funcExpr, argExpr, range)) -> body
| (SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr): SynExpr) -> body
| (SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) as app) -> body
| (firstLongIdentifier, secondLongIdentifier, thirdLongIdentifier) -> body
| ([ firstLongIdentifier; secondLongIdentifier; thirdLongIdentifier ]) -> body
| ({ FirstField = firstField; SecondField = secondField; ThirdField = thirdField }) -> body
| (Some(SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr))) -> body
| ((SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr))) -> body
| (SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) as app), otherArgument -> body
| (SynExpr.App(argExpr = argExpr): SomeVeryLongTypeNameThatDoesNotFitBehindTheCloser) -> body
| (SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) as someVeryLongAliasThatDoesNotFitBehindTheCloserOfThePattern) -> body
| (SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) as SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr)) -> body
| (Some x) -> body
| _ -> ()
