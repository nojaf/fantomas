match ast with
| Some(
    ParsedImplFileInput(
        modules = modules
        isScript = isScript
        fileName = fileName
    )
  ) ->
    body
| Some(
    Some(
        ParsedImplFileInput(
            modules = modules
            isScript = isScript
            fileName = fileName
        )
    )
  ) ->
    body
| _ -> ()
