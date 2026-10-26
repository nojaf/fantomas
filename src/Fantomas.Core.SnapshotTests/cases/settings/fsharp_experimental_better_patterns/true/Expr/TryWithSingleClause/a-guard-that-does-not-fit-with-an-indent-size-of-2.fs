(*---
# With an indent of 2, the condition of a guard that does not fit goes two levels in from `with`.
fsharp_experimental_better_patterns = true
indent_size = 2
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
