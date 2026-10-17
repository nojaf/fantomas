match x with
| A
| B ->
    body
| Some(A | B) -> body
| _ -> ()
