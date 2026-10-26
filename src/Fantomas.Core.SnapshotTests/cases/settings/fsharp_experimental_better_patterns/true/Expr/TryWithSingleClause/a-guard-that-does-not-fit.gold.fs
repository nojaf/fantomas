let afterAMultilinePattern () =
    try
        body
    with SynExpr.App(
            isInfix = true
            funcExpr = funcExpr
            argExpr = argExpr
        ) when
        funcExpr.Range.StartLine <> argExpr.Range.EndLine
        && isInfix ->
        handler

let afterAShortPattern () =
    try
        body
    with SynExpr.Ident ident when
        ident.idText.StartsWith("op_")
        && not (isOperatorWithoutSpace ident.idText) ->
        handler
