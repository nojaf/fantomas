module Fantomas.Tests.BetweenTheBars

open NUnit.Framework
open FsUnit
open Fantomas.Tests.TestHelper

[<Test>]
let ``two simple let bindings`` () =
    formatSourceString
        false
        """
let a =    1
let b   =  2
"""
        config
    |> prepend newline
    |> should
        equal
        """
let a = 1
let b = 2
"""

[<Test>]
let ``comment between simple let bindings`` () =
    formatSourceString
        false
        """
let a =    1
//   foobar
let b   =  2
"""
        config
    |> prepend newline
    |> should
        equal
        """
let a = 1
//   foobar
let b = 2
"""
