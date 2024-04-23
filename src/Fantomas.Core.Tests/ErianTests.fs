module Fantomas.Core.Tests.ErianTests

open Fantomas.Core
open NUnit.Framework
open FsUnit
open Fantomas.Core.Tests.TestHelpers

let config =
    { config with
        ExperimentalErian = true
        MultilineBracketStyle = Stroustrup }

[<Test>]
let ``testing the waters`` () =
    formatSourceString
        """
type Person =
    { id : ID
      name : string
      age : int
      nationality : Nation option
      likesPho : bool }
"""
        config
    |> prepend newline
    |> should
        equal
        """
type Person = {
    id          : ID
    name        : string
    age         : int
    nationality : Nation option
    likesPho    : bool
}
"""

[<Test>]
let ``record expression`` () =
    formatSourceString
        """
let rei =
    { id = newID()
      name = "rei"
      age = 33
      nationality = Some(Nations.Japan)
      likesPho = true }
"""
        config
    |> prepend newline
    |> should
        equal
        """
let rei = {
    id          = newID ()
    name        = "rei"
    age         = 33
    nationality = Some(Nations.Japan)
    likesPho    = true
}
"""
