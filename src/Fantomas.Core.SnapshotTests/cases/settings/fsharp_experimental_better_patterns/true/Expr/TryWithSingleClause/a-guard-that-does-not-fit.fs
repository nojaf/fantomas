(*---
# A guard that does not fit breaks after `when`, and its condition goes one indent level in from
# `with`.
fsharp_experimental_better_patterns = true
max_line_length = 60
---*)
let afterAMultilinePattern () =
    try
        body
    with SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) when funcExpr.Range.StartLine <> argExpr.Range.EndLine && isInfix ->
        handler

let afterAShortPattern () =
    try
        body
    with SynExpr.Ident ident when ident.idText.StartsWith("op_") && not (isOperatorWithoutSpace ident.idText) ->
        handler
