let list () =
    try
        body
    with Failure
            [
                firstLongIdentifier
                secondLongIdentifier
                thirdLongIdentifierXYZ
            ] ->
        handler

let listInNamedField () =
    try
        body
    with SynExpr.ArrayOrList(
            isArray = false
            exprs =
                [
                    firstExpression
                    secondExpression
                    thirdExpression
                ]
        ) ->
        handler
