match e with
| SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExpr = argExpr
  ) ->
    body
| _ -> ()
