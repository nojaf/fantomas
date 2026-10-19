match e with
| [ SynBinding(
        headPat = SynPat.Named(ident = ident)
        expr = expr
    )
    SynBinding(
        headPat = SynPat.Named(ident = ident)
        expr = expr
    ) ] ->
    body
| [| firstItemWithALongName
     secondItemWithALongName
     thirdItemWithALongName |] as items ->
    body
| [ SynExpr.Ident firstIdentifier
     | SynExpr.Ident secondIdentifier
     | SynExpr.Ident thirdIdentifier ] ->
    body
| [ a; b ] -> body
| [] -> ()
