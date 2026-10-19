match e with
| [
    SynBinding(
        headPat = SynPat.Named(ident = ident)
        expr = expr
    )
    SynBinding(
        headPat = SynPat.Named(ident = ident)
        expr = expr
    )
  ] ->
    body
| [|
    firstItemWithALongName
    secondItemWithALongName
    thirdItemWithALongName
  |] as items ->
    body
| [ a; b ] -> body
| [] -> ()
