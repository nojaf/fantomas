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

[<Test>]
let ``named module with single let binding`` () =
    formatSourceString
        false
        """
module  Foo

let add x  y = x +  y
"""
        config
    |> prepend newline
    |> should
        equal
        """
module Foo

let add x y = x + y
"""

[<Test>]
let ``named module with comment and single let binding`` () =
    formatSourceString
        false
        """
module  Foo
// bar
let add x  y = x +  y
"""
        config
    |> prepend newline
    |> should
        equal
        """
module Foo
// bar
let add x y = x + y
"""

[<Test>]
let ``empty module`` () =
    formatSourceString
        false
        """
module  FooBar
"""
        config
    |> prepend newline
    |> should
        equal
        """
module FooBar
"""

[<Test>]
let ``private module`` () =
    formatSourceString
        false
        """
module private Foobar

let a =9
let b= 10
"""
        config
    |> prepend newline
    |> should
        equal
        """
module private Foobar

let a = 9
let b = 10
"""

[<Test>]
let ``simple namespace`` () =
    formatSourceString
        false
        """
namespace   Company.Product.Domain.Project

let a =  42
"""
        config
    |> prepend newline
    |> should
        equal
        """
namespace Company.Product.Domain.Project

let a = 42
"""

[<Test>]
let ``recursive module`` () =
    formatSourceString
        false
        """
module   rec   Foobar

type X =   int
"""
        config
    |> prepend newline
    |> should
        equal
        """
module rec Foobar

type X = int
"""

[<Test>]
let ``recursive namespace`` () =
    formatSourceString
        false
        """
namespace   rec   Foobar

type X =   int
"""
        config
    |> prepend newline
    |> should
        equal
        """
namespace rec Foobar

type X = int
"""

[<Test>]
let ``global namespace`` () =
    formatSourceString
        false
        """
namespace  global

type X = int
"""
        config
    |> prepend newline
    |> should
        equal
        """
namespace global

type X = int
"""

[<Test>]
let ``signature namespace with single type`` () =
    formatSourceString
        true
        """
namespace  Foo

type X =
    {
        Y: int
        Z: string
    }
"""
        config
    |> prepend newline
    |> should
        equal
        """
namespace Foo

type X = { Y: int; Z: string }
"""

[<Test>]
let ``module with attribute`` () =
    formatSourceString
        false
        """
[<  Foo  >]
module Bar

let s = "meh"
"""
        config
    |> prepend newline
    |> should
        equal
        """
[<Foo>]
module Bar

let s = "meh"
"""

[<Test>]
let ``signature file module with multiple attribute and comment`` () =
    formatSourceString
        true
        """
//    multiple attributes
[<  Foo  >]
[< EvenFooer     >]
module   Bar

val s :   int -> string
"""
        config
    |> prepend newline
    |> should
        equal
        """
//    multiple attributes
[<Foo>]
[<EvenFooer>]
module Bar

val s : int -> string
"""

[<Test>]
let ``no existing newline in source`` () =
    formatSourceString
        false
        """
module X
let a = 40
"""
        config
    |> prepend newline
    |> should
        equal
        """
module X

let a = 40
"""

[<Test>]
let ``single line let binding and multiline let binding`` () =
    formatSourceString
        false
        """
module X
let a = 14
let sum x y z =
    // some complex math
    x + y + z
"""
        config
    |> prepend newline
    |> should
        equal
        """
module X

let a = 14

let sum x y z =
    // some complex math
    x + y + z
"""

[<Test>]
let ``comment in let binding in anon module`` () =
    formatSourceString
        false
        """
let sum x y z =
    // some complex math
    x + y + z
"""
        config
    |> prepend newline
    |> should
        equal
        """
let sum x y z =
    // some complex math
    x + y + z
"""

[<Test>]
let ``attribute above let binding`` () =
    formatSourceString
        false
        """
[<Foo >]
let bar =  7
"""
        config
    |> prepend newline
    |> should
        equal
        """
[<Foo>]
let bar = 7
"""

[<Test>]
let ``attribute above type`` () =
    formatSourceString
        false
        """
[<Foo >]
type Person =
    { Name: string
      Age:  int }
"""
        config
    |> prepend newline
    |> should
        equal
        """
[<Foo>]
type Person = { Name: string; Age: int }
"""

(* TODO:
- other attibutes from module: LongIdent * bool * SynModuleOrNamespaceKind * PreXmlDoc * SynAttributes * range
- Attributes are not part of the range of an SynModuleDecl, consider custom range?
- Tests without newline in original source
- StrictMode
*)
