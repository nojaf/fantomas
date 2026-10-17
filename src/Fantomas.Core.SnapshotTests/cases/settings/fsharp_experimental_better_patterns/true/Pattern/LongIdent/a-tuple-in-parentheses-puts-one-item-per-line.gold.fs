match e with
| SynExpr.App(
    ExprAtomicFlag.NonAtomic,
    false,
    SynExpr.Ident funcIdent,
    argumentExpression,
    range
  ) ->
    body
| GetSetMember(
    Some(SynBinding(accessibility = getterAccessibility)),
    Some(setterBinding)
  ) ->
    body
| Some(a, b) -> body
| _ -> ()
