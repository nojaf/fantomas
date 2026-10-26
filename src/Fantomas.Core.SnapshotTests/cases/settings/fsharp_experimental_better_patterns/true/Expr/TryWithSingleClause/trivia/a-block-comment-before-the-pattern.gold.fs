let blockCommentBeforeThePattern () =
    try
        body
    with (* trivia *) SynExpr.App(
                            isInfix = true
                            funcExpr = funcExpr
                            argExpr = argExpr
                        ) ->
        handler
