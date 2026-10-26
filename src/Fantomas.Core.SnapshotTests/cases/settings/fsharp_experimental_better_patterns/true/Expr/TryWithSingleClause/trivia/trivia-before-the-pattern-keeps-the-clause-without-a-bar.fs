(*---
# Trivia before the pattern puts it on a line of its own, on the column of `with`. It gets no bar,
# as a clause after `with ` gets none, and is laid out from where it starts.
fsharp_experimental_better_patterns = true
max_line_length = 60
---*)
let lineComment () =
    try
        body
    with
    // trivia
    SynExpr.App(isInfix = true; funcExpr = funcExpr; argExpr = argExpr) ->
        handler

let typeTest () =
    try
        foo.CreationTime <> defaultTime
    with
    // trivia
    :? FileNotFoundException as e -> false

let blockComment () =
    try
        body
    with
    (* trivia *)
    Failure message -> message

let blankLines () =
    try
        body
    with


    Failure message -> message

let insideMatchClause () =
    match x with
    | A ->
        try
            body
        with
        // trivia
        Failure message -> message
    | B -> b
