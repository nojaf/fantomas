match e with
| SynExpr.Ident firstIdentifier,
  SynExpr.Ident secondIdentifier,
  SynExpr.Ident thirdIdentifier
    ->
    body
| SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExpr = argExpr
  ),
  SynExpr.Ident secondIdentifier
    ->
    body
| ParseArguments
    firstArgumentPattern
    secondArgumentPattern
    thirdArgumentPattern
    ->
    body
| a, b -> body
| _ -> ()
