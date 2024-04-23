module Fantomas.Core.Tests.ErianTests

open Fantomas.Core
open NUnit.Framework
open FsUnit
open Fantomas.Core.Tests.TestHelpers

let config =
    { config with
        ExperimentalErian = true
        MultilineBracketStyle = Aligned }

[<Test>]
let ``testing the waters`` () =
    formatSourceString
        """
type R =
    {
        Xemnas: int
        Xigbar: int
        Xaldin: int
        Vexen: int
        Lexaeus: int
        Zexion: int
        Saïx: int
        Axel: int
        Demyx: int
        Luxord: int
        Marluxia: int
        Larxene: int
        Roxas: int
    }
"""
        config
    |> prepend newline
    |> should
        equal
        """
type R =
    {
        Xemnas:   int
        Xigbar:   int
        Xaldin:   int
        Vexen:    int
        Lexaeus:  int
        Zexion:   int
        Saïx:     int
        Axel:     int
        Demyx:    int
        Luxord:   int
        Marluxia: int
        Larxene:  int
        Roxas:    int
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
        { config with
            MultilineBracketStyle = Stroustrup }
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
