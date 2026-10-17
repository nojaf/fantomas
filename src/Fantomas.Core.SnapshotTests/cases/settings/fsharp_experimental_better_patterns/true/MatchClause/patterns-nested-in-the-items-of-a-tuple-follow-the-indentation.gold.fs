match e with
| SynExpr.App(
    ExprAtomicFlag.NonAtomic,
    false,
    SynExpr.Ident funcIdent,
    argumentExpression,
    range
  ),
  Some(
    SynBinding(
        accessibility = getterAccessibility
        attributes = attributes
    )
  )
    ->
    body
| Some(
    SynExpr.App(
        ExprAtomicFlag.NonAtomic,
        false,
        SynExpr.Ident funcIdent,
        argumentExpression,
        range
    )
  ),
  ParseArguments
    firstArgumentPattern
    secondArgumentPattern
    thirdArgumentPattern
    ->
    body
| ParseArguments
    (Some(
        SynExpr.App(
            ExprAtomicFlag.NonAtomic,
            false,
            funcExpr,
            argExpr,
            range
        )
    ))
    secondArgumentPattern,
  other
    ->
    body
| _ -> ()
