match e with
| (
    SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExpr = argExpr
    )
  ) ->
    body
| (
    SynExpr.App(
        ExprAtomicFlag.NonAtomic,
        false,
        funcExpr,
        argExpr,
        range
    )
  ) ->
    body
| (
    SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExpr = argExpr
    ): SynExpr
  ) ->
    body
| (
    SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExpr = argExpr
    ) as app
  ) ->
    body
| (
    firstLongIdentifier,
    secondLongIdentifier,
    thirdLongIdentifier
  ) ->
    body
| (
    [
        firstLongIdentifier
        secondLongIdentifier
        thirdLongIdentifier
    ]
  ) ->
    body
| (
    {
        FirstField = firstField
        SecondField = secondField
        ThirdField = thirdField
    }
  ) ->
    body
| (
    Some(
        SynExpr.App(
            isInfix = true
            funcExpr = funcExpr
            argExpr = argExpr
        )
    )
  ) ->
    body
| (
    (SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExpr = argExpr
    ))
  ) ->
    body
| (
    SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExpr = argExpr
    ) as app
  ),
  otherArgument ->
    body
| (
    SynExpr.App(argExpr = argExpr):
        SomeVeryLongTypeNameThatDoesNotFitBehindTheCloser
  ) ->
    body
| (
    SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExpr = argExpr
    ) as
        someVeryLongAliasThatDoesNotFitBehindTheCloserOfThePattern
  ) ->
    body
| (
    SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExpr = argExpr
    ) as
        SynExpr.App(
            isInfix = true
            funcExpr = funcExpr
            argExpr = argExpr
        )
  ) ->
    body
| (Some x) -> body
| _ -> ()
