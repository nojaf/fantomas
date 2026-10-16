(*---
# https://github.com/fsprojects/fantomas/issues/2670
fsharp_experimental_better_patterns = true
max_line_length = 60
---*)
match x with
| SynExpr.ObjExpr(
    objType = objType
    argOptions = argOptions
    extraImpls = extraImpls123456780) -> body
| _ -> ()
