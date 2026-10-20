match e with
| SynExpr.App(
    ExprAtomicFlag.NonAtomic,
    false,
    funcExpr,
    argExpr,
    range
  ) as application ->
  body
| SynExpr.Ident identifier as
    SynExpr.App(
      ExprAtomicFlag.NonAtomic,
      false,
      funcExpr,
      argExpr,
      range
    ) ->
  body
| SynExpr.Ident someLongIdentifierName as
    someEvenLongerAliasForTheWholeThing ->
  body
| SynExpr.Ident x as y -> body
| SynExpr.Ident someLongIdentifierName as
    someEvenLongerAliasForTheWholeThing,
  otherArgument ->
  body
| _ -> ()
