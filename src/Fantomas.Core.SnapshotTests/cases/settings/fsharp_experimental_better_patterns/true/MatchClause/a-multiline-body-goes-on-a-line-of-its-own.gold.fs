match e with
| SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExpr = argExpr
  ) ->
    visitChainLinks funcExpr argExpr (fun links ->
        continuation links)
| SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExpr = argExpr
  ) ->
    if isInfix then
        mkInfixApp funcExpr argExpr
    else
        mkApp funcExpr argExpr
| SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExpr = argExpr
  ) ->
    let funcNode = mkExpr funcExpr
    ExprAppNode(funcNode, mkExpr argExpr)
| SynExpr.App(funcExpr = funcExpr) ->
    visitChainLinks funcExpr argExpr (fun links ->
        continuation links)
| SynExpr.Ident ident ->
    if isInfix then
        mkInfixApp funcExpr argExpr
    else
        mkApp funcExpr argExpr
| _ -> ()
