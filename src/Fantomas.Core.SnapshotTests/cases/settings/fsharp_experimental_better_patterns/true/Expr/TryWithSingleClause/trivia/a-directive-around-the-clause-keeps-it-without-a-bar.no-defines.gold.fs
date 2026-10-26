let f () =
    try
        body
    with
    #if DEBUG
    #else
    _ ->
        fallback
#endif
