match e with
| x :: rest -> body
| firstIdentifier
  :: secondIdentifier
  :: thirdIdentifier
  :: rest
  ->
  body
| SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExpr = argExpr
  )
  :: rest
  ->
  body
| head
  :: SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExpr = argExpr
  )
  ->
  body
| SynExpr.App(
    ExprAtomicFlag.NonAtomic,
    false,
    funcExpr,
    argExpr,
    range
  )
  :: rest
  ->
  body
| head
  :: SynExpr.App(
    ExprAtomicFlag.NonAtomic,
    false,
    funcExpr,
    argExpr,
    range
  )
  ->
  body
| SynExpr.App(isInfix = true; argExpr = argExpr)
  :: SynExpr.Lambda(
    fromMethod = fromMethod
    body = lambdaBody
  )
  :: rest
  ->
  body
| [
    SynExpr.Ident firstIdentifier
    SynExpr.Ident secondIdentifier
  ]
  :: rest
  ->
  body
| SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExpr = argExpr
  )
  :: rest,
  otherArgument
  ->
  body
| (
    SynExpr.App(
      isInfix = true
      funcExpr = funcExpr
      argExpr = argExpr
    )
    :: rest
  )
  ->
  body
| _ -> ()
