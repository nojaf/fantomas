let f () =
    try
        body
    with
#if DEBUG
    SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExpr = argExpr
    ) ->
        handler
#else
    _ ->
        fallback
#endif
