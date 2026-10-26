(*---
# A block comment between `with` and the pattern moves the pattern right. The closer still steps in
# by whole indent levels until it is no more than one column left of where the pattern starts.
fsharp_experimental_better_patterns = true
max_line_length = 60
---*)
let blockCommentBeforeThePattern () =
    try
        body
    with (* trivia *) SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) ->
        handler
