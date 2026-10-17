match e with
| SynExpr.App(
    isInfix = true
    argExprWithALongName = argExpr
  )
    ->
    body
| SomeVeryLongUnionCaseNameNumberOne
| SomeVeryLongUnionCaseNameNumberTwo
    ->
    body
| _ -> ()
