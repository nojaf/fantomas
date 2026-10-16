match x with
| SomeCase(
    firstArgumentPattern = a
    secondArgumentPattern = b
  )
| OtherCase(
    firstArgumentPattern = a
    secondArgumentPattern = b
  ) -> body
| _ -> ()
