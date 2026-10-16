match e with
| SynExpr.App(
    isInfix = true
    funcExpr =
        SynExpr.LongIdent(longDotId = longDotIdentifier1)
    argExpr = argExpr
  ) ->
    body
| SynExpr.App(
    isInfix = true
    funcExpr =
        SynExpr.LongIdent(
            longDotId = longDotId
            extra = extra
        )
    argExpr = argExpr
  ) ->
    body
| _ -> ()
