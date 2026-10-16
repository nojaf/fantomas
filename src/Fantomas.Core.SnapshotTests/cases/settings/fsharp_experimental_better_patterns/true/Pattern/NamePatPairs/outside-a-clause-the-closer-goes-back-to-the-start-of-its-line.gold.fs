let (SomeCase(
    firstArgumentPattern = a
    secondArgumentPattern = b
)) =
    x

let f =
    fun
        (SomeCase(
            firstArgumentPattern = a
            secondArgumentPattern = b
        )) -> a
