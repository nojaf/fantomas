match e with
| (
    SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExpr = argExpr
    ) : SynExpr
  ) ->
    body
| (
    SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExpr = argExpr
    ) : SynExpr
  ),
  otherArgument ->
    body
| _ -> ()
