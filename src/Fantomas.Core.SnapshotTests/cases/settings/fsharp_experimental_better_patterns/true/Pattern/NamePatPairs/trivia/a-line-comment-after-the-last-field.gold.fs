match x with
| SomeCase(
    firstArgumentPattern = a
    secondArgumentPattern = b // the second one
  ) ->
    body
| _ -> ()
