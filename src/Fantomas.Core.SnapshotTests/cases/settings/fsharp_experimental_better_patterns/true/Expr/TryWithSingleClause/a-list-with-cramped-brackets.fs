(*---
# A list after the name of the pattern, or in one of its named fields, keeps the layout cramped
# brackets give it.
fsharp_experimental_better_patterns = true
fsharp_multiline_bracket_style = cramped
max_line_length = 60
---*)
let list () =
    try
        body
    with Failure [ firstLongIdentifier; secondLongIdentifier; thirdLongIdentifierXYZ ] ->
        handler

let listInNamedField () =
    try
        body
    with SynExpr.ArrayOrList(isArray = false; exprs = [ firstExpression; secondExpression; thirdExpression ]) ->
        handler
