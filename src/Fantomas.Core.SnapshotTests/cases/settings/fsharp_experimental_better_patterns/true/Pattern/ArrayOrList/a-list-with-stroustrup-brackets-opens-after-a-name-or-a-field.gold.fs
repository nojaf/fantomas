match e with
| Some([
    SynExpr.Ident firstIdentifier
    SynExpr.Ident secondIdentifier
    SynExpr.Ident thirdIdentifier
  ]) ->
    body
| Some [
    SynExpr.Ident firstIdentifier
    SynExpr.Ident secondIdentifier
    SynExpr.Ident thirdIdentifier
  ] ->
    body
| SynExpr.Tuple(
    false,
    [
        SynExpr.Ident firstIdentifier
        SynExpr.Ident secondIdentifier
    ],
    commaRanges,
    range
  ) ->
    body
| ParseArguments
    [
        SynExpr.Ident firstIdentifier
        SynExpr.Ident secondIdentifier
    ]
    otherArgument ->
    body
| [
    [
        SynExpr.Ident firstIdentifier
        SynExpr.Ident secondIdentifier
    ]
    [ SynExpr.Ident thirdIdentifier ]
  ] ->
    body
| SynModuleDecl.Let(
    bindings = [
        SynBinding(
            headPat = SynPat.Named(ident = ident)
            expr = expr
        ) as binding
    ]
  ) ->
    body
| Some [| a; b |] -> body
| _ -> ()
