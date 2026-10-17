match e with
| Some(
    SynExpr.App(
      isInfix = true
      funcExpr = funcExpr
      argExpr = argExpr
    )
  ) ->
  body
| _ -> ()
