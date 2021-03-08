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

[<Test>]
let ``newline between simple let bindings`` () =
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
let ``three simple let bindings interlaced with comments`` () =
    formatSourceString
        false
        """
let a =  9
// foobar
let b = "meh"
(* booyah *)
let c = 99
"""
        config
    |> prepend newline
    |> should
        equal
        """
let a = 9
// foobar
let b = "meh"
(* booyah *)
let c = 99
"""

[<Test>]
let ``single simple let binding with leading comment`` () =
    formatSourceString
        false
        """
(* great function  *)
let sum a b = a +  b
"""
        config
    |> prepend newline
    |> should
        equal
        """
(* great function  *)
let sum a b = a + b
"""

[<Test>]
let ``trim leading newline before first let binding`` () =
    formatSourceString
        false
        """


let a =  0
"""
        config
    |> prepend newline
    |> should
        equal
        """
let a = 0
"""

[<Test>]
let ``trim leading newlines above first comment above first let binding`` () =
    formatSourceString
        false
        """


// foobar

let moo () = printfn "moohhhh"
"""
        config
    |> prepend newline
    |> should
        equal
        """
// foobar

let moo () = printfn "moohhhh"
"""

(* TODO:
- Named modules, namespaces
- Multiple modules
- Signature files
- Attributes are not part of the range of an SynModuleDecl, consider custom range?
*)