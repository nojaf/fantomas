(*---
# With an indent of 2, what is inside the parentheses of an item in a :: chain goes right of its
# closer.
fsharp_experimental_better_patterns = true
fsharp_align_function_signature_to_indentation = true
indent_size = 2
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
