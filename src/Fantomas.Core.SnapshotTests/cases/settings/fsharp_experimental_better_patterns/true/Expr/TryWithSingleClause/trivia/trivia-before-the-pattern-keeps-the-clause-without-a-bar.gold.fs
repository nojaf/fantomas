let lineComment () =
    try
        body
    with
    // trivia
    SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExpr = argExpr
    ) ->
        handler

let typeTest () =
    try
        foo.CreationTime <> defaultTime
    with
    // trivia
    :? FileNotFoundException as e ->
        false

let blockComment () =
    try
        body
    with
    (* trivia *)
    Failure message ->
        message

let blankLines () =
    try
        body
    with


    Failure message ->
        message

let insideMatchClause () =
    match x with
    | A ->
        try
            body
        with
        // trivia
        Failure message ->
            message
    | B -> b
