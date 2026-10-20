match e with
| Some(
    SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExpr = argExpr
    ) as app
  ) ->
    body
| Some(
    SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExpr = argExpr
    ) as
        someVeryLongAliasThatDoesNotFitBehindTheCloserOfThePattern
  ) ->
    body
| Some(
    SynExpr.Ident identifier as
        SynExpr.App(
            ExprAtomicFlag.NonAtomic,
            false,
            funcExpr,
            argExpr,
            range
        )
  ) ->
    body
| Some(
    SynExpr.Ident someLongIdentifierName as
        someEvenLongerAliasForTheWholeThing
  ) ->
    body
| _ -> ()
