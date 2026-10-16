match e with
| SynExpr.App(
    isInfix = true
    funcExpr =
      SynExpr.LongIdent(
        longDotId = longDotId
        extra = extra
      )
    argExpr = argExpr
  ) -> body
| _ -> ()
