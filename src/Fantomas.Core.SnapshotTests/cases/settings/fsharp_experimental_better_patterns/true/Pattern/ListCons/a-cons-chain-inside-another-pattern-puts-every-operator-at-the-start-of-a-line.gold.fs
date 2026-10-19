match e with
| Some(
    SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExpr = argExpr
    )
    :: rest
  ) ->
    body
| ParseArguments
    (SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExpr = argExpr
    )
    :: rest)
    otherArgument ->
    body
| SynModuleDecl.Let(
    bindings =
        SynBinding(
            headPat = SynPat.Named(ident = ident)
            expr = expr
        )
        :: rest
  ) ->
    body
| [
    SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExpr = argExpr
    )
    :: rest
    otherList
  ] ->
    body
| _ -> ()
