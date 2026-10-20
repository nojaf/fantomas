match e with
| ParseArguments
    (Some(
        SynExpr.App(
            isInfix = true
            funcExpr = funcExpr
            argExpr = argExpr
        )
    ))
    otherArgument ->
    body
| ParseArguments
    (SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExpr = argExpr
    ) as app)
    otherArgument ->
    body
| Some(
    (SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExpr = argExpr
    ) as app),
    otherArgument
  ) ->
    body
| SynModuleDecl.Let(
    bindings =
        (SynBinding(
            headPat = SynPat.Named(ident = ident)
            expr = expr
        ) as binding)
        :: rest
  ) ->
    body
| [
    (SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExpr = argExpr
    ) as app)
    otherItem
  ] ->
    body
| _ -> ()
