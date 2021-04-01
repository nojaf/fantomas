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

[<Test>]
let ``hash directive block above let binding, no defines`` () =
    formatSourceStringWithDefines
        []
        """
module MyApp

#if DEBUG
printfn "DEBUG"
#endif

let e2e value =
    Props.Data("e2e", value)
"""
        config
    |> prepend newline
    |> should
        equal
        """
module MyApp

#if DEBUG

#endif

let e2e value = Props.Data("e2e", value)
"""

[<Test>]
let ``non active code should not be used between bindings, DEBUG`` () =
    formatSourceStringWithDefines
        [ "DEBUG" ]
        """
module MyApp

#if DEBUG
[<Emit("console.log('%c' +  $1, 'color: ' + $0)")>]
let printInColor (color:string) (msg:string):unit = jsNative
#endif

let e2e value =
    Props.Data("e2e", value)
"""
        config
    |> prepend newline
    |> should
        equal
        """
module MyApp

#if DEBUG
[<Emit("console.log('%c' +  $1, 'color: ' + $0)")>]
let printInColor (color: string) (msg: string) : unit = jsNative
#endif

let e2e value = Props.Data("e2e", value)
"""

[<Test>]
let ``non active code should not be used between bindings`` () =
    formatSourceString
        false
        """
module MyApp

#if DEBUG
[<Emit("console.log('%c' +  $1, 'color: ' + $0)")>]
let printInColor (color:string) (msg:string):unit = jsNative
#endif

let e2e value =
    Props.Data("e2e", value)
"""
        config
    |> prepend newline
    |> should
        equal
        """
module MyApp

#if DEBUG
[<Emit("console.log('%c' +  $1, 'color: ' + $0)")>]
let printInColor (color: string) (msg: string) : unit = jsNative
#endif

let e2e value = Props.Data("e2e", value)
"""

[<Test>]
let ``comments after anon module`` () =
    formatSourceString
        false
        """
printfn "%s"   @"c:\def\ghi\jkl"
printfn "%s"    "c:\\def\\ghi\\jkl"

(*
xyz
*)
"""
        config
    |> prepend newline
    |> should
        equal
        """
printfn "%s" @"c:\def\ghi\jkl"
printfn "%s" "c:\\def\\ghi\\jkl"

(*
xyz
*)
"""

[<Test>]
let ``comments between SynModuleOrNamespaces`` () =
    formatSourceString
        false
        """
// Leading comment

namespace Foo

let x =    7

(* comment between Foo and Bar *)

namespace Bar

let y =         8

// Closing comment
"""
        config
    |> prepend newline
    |> should
        equal
        """
// Leading comment

namespace Foo

let x = 7

(* comment between Foo and Bar *)

namespace Bar

let y = 8

// Closing comment
"""

[<Test>]
let ``add blank line between multiline type and let binding`` () =
    formatSourceString
        false
        """
type T() =
    member __.Property = "hello"
let longNamedFunlongNamedFunlongNamedFunlongNamedFunlongNamedFun (x:T) = x
"""
        config
    |> prepend newline
    |> should
        equal
        """
type T() =
    member __.Property = "hello"

let longNamedFunlongNamedFunlongNamedFunlongNamedFunlongNamedFun (x: T) = x
"""

[<Test>]
let ``combine Attributes and DoExpr (unit) as single expression`` () =
    formatSourceString
        false
        """
[<assembly: CLSCompliant(true)>]
[<assembly: ComVisible(false)>]
[<assembly: AssemblyTitle("AltCover.Visualizer")>]
[<assembly: AssemblyDescription("Coverage and static analysis visualizer for NCover (possibly extended) and OpenCover")>]
[<assembly: System.Resources.NeutralResourcesLanguageAttribute("en-GB")>]
()
"""
        config
    |> prepend newline
    |> should
        equal
        """
[<assembly: CLSCompliant(true)>]
[<assembly: ComVisible(false)>]
[<assembly: AssemblyTitle("AltCover.Visualizer")>]
[<assembly: AssemblyDescription("Coverage and static analysis visualizer for NCover (possibly extended) and OpenCover")>]
[<assembly: System.Resources.NeutralResourcesLanguageAttribute("en-GB")>]
()
"""

[<Test>]
let ``multiple open statements should be separated with blank lines`` () =
    formatSourceString
        false
        """
module Foobar
open System
open System.IO
let x =   8
"""
        config
    |> prepend newline
    |> should
        equal
        """
module Foobar

open System
open System.IO

let x = 8
"""

[<Test>]
let ``add newlines between #r and next let binding`` () =
    formatSourceString
        false
        """
#r @"C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\5.0.2\Microsoft.Extensions.Hosting.dll"
#r @"C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\5.0.2\Microsoft.Extensions.ObjectPool.dll"
let foo = "bar"
"""
        config
    |> prepend newline
    |> should
        equal
        """
#r @"C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\5.0.2\Microsoft.Extensions.Hosting.dll"
#r @"C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\5.0.2\Microsoft.Extensions.ObjectPool.dll"

let foo = "bar"
"""

[<Test>]
let ``comment between open statements`` () =
    formatSourceString
        false
        """
open Foo
// open Bar
open FooBar

let x = 90
"""
        config
    |> prepend newline
    |> should
        equal
        """
open Foo
// open Bar
open FooBar

let x = 90
"""

[<Test>]
let ``attribute above type in signature file`` () =
    formatSourceString
        true
        """
namespace Fantomas

open Fantomas.FormatConfig
open Fantomas.SourceOrigin
open FSharp.Compiler.Text
open FSharp.Compiler.SourceCodeServices
open FSharp.Compiler.SyntaxTree

[<Sealed>]
type CodeFormatter =
    /// Parse a source string using given config
    static member ParseAsync :
        fileName: string * source: SourceOrigin * parsingOptions: FSharpParsingOptions * checker: FSharpChecker ->
        Async<(ParsedInput * string list) array>

    /// Format an abstract syntax tree using an optional source for trivia processing
    static member FormatASTAsync :
        ast: ParsedInput * fileName: string * defines: string list * source: SourceOrigin option * config: FormatConfig ->
        Async<string>
"""
        config
    |> prepend newline
    |> should
        equal
        """
namespace Fantomas

open Fantomas.FormatConfig
open Fantomas.SourceOrigin
open FSharp.Compiler.Text
open FSharp.Compiler.SourceCodeServices
open FSharp.Compiler.SyntaxTree

[<Sealed>]
type CodeFormatter =
    /// Parse a source string using given config
    static member ParseAsync :
        fileName: string * source: SourceOrigin * parsingOptions: FSharpParsingOptions * checker: FSharpChecker ->
        Async<(ParsedInput * string list) array>

    /// Format an abstract syntax tree using an optional source for trivia processing
    static member FormatASTAsync :
        ast: ParsedInput * fileName: string * defines: string list * source: SourceOrigin option * config: FormatConfig ->
        Async<string>
"""

[<Test>]
let ``hash token in signature file`` () =
    formatSourceString
        true
        """
namespace GreatProjectThing

#if DEBUG
type Meh =
        class
        end
#endif

type Foo =
    member Engage : string ->    int
"""
        config
    |> prepend newline
    |> should
        equal
        """
namespace GreatProjectThing

#if DEBUG
type Meh =
    class
    end
#endif

type Foo =
    member Engage : string -> int
"""

[<Test>]
let ``#else block after source code`` () =
    formatSourceString
        false
        """
namespace ExtCore

type substring =
    static member CompareOrdinal (strA : substring, strB : substring) =
        if strA.Length = 0 && strB.Length = 0 then 0
        elif strA.String == strB.String && strA.Offset = strB.Offset then
            compare strA.Length strB.Length
        else
#if INVARIANT_CULTURE_STRING_COMPARISON
            System.String.Compare (
                strA.String, strA.Offset,
                strB.String, strB.Offset,
                min strA.Length strB.Length,
                false,
                CultureInfo.InvariantCulture)
#else
            System.String.CompareOrdinal (
                strA.String, strA.Offset,
                strB.String, strB.Offset,
                min strA.Length strB.Length)
#endif
"""
        config
    |> prepend newline
    |> should
        equal
        """
namespace ExtCore

type substring =
    static member CompareOrdinal(strA: substring, strB: substring) =
        if strA.Length = 0 && strB.Length = 0 then
            0
        elif strA.String == strB.String
             && strA.Offset = strB.Offset then
            compare strA.Length strB.Length
        else
#if INVARIANT_CULTURE_STRING_COMPARISON
            System.String.Compare(
                strA.String,
                strA.Offset,
                strB.String,
                strB.Offset,
                min strA.Length strB.Length,
                false,
                CultureInfo.InvariantCulture
            )
#else
            System.String.CompareOrdinal(
                strA.String,
                strA.Offset,
                strB.String,
                strB.Offset,
                min strA.Length strB.Length
            )
#endif
"""

[<Test>]
let ``#else block after source code, no defines`` () =
    formatSourceStringWithDefines
        []
        """
namespace ExtCore

type substring =
    static member CompareOrdinal (strA : substring, strB : substring) =
        if strA.Length = 0 && strB.Length = 0 then 0
        elif strA.String == strB.String && strA.Offset = strB.Offset then
            compare strA.Length strB.Length
        else
#if INVARIANT_CULTURE_STRING_COMPARISON
            System.String.Compare (
                strA.String, strA.Offset,
                strB.String, strB.Offset,
                min strA.Length strB.Length,
                false,
                CultureInfo.InvariantCulture)
#else
            System.String.CompareOrdinal (
                strA.String, strA.Offset,
                strB.String, strB.Offset,
                min strA.Length strB.Length)
#endif
"""
        config
    |> prepend newline
    |> should
        equal
        """
namespace ExtCore

type substring =
    static member CompareOrdinal(strA: substring, strB: substring) =
        if strA.Length = 0 && strB.Length = 0 then
            0
        elif strA.String == strB.String
             && strA.Offset = strB.Offset then
            compare strA.Length strB.Length
        else
#if INVARIANT_CULTURE_STRING_COMPARISON






#else
            System.String.CompareOrdinal(
                strA.String,
                strA.Offset,
                strB.String,
                strB.Offset,
                min strA.Length strB.Length
            )
#endif
"""

[<Test>]
let ``#else block after source code, INVARIANT_CULTURE_STRING_COMPARISON`` () =
    formatSourceStringWithDefines
        [ "INVARIANT_CULTURE_STRING_COMPARISON" ]
        """
namespace ExtCore

type substring =
    static member CompareOrdinal (strA : substring, strB : substring) =
        if strA.Length = 0 && strB.Length = 0 then 0
        elif strA.String == strB.String && strA.Offset = strB.Offset then
            compare strA.Length strB.Length
        else
#if INVARIANT_CULTURE_STRING_COMPARISON
            System.String.Compare (
                strA.String, strA.Offset,
                strB.String, strB.Offset,
                min strA.Length strB.Length,
                false,
                CultureInfo.InvariantCulture)
#else
            System.String.CompareOrdinal (
                strA.String, strA.Offset,
                strB.String, strB.Offset,
                min strA.Length strB.Length)
#endif
"""
        config
    |> prepend newline
    |> should
        equal
        """
namespace ExtCore

type substring =
    static member CompareOrdinal(strA: substring, strB: substring) =
        if strA.Length = 0 && strB.Length = 0 then
            0
        elif strA.String == strB.String
             && strA.Offset = strB.Offset then
            compare strA.Length strB.Length
        else
#if INVARIANT_CULTURE_STRING_COMPARISON
            System.String.Compare(
                strA.String,
                strA.Offset,
                strB.String,
                strB.Offset,
                min strA.Length strB.Length,
                false,
                CultureInfo.InvariantCulture
            )
#else




#endif
"""

(* TODO:
- other attibutes from module: LongIdent * bool * SynModuleOrNamespaceKind * PreXmlDoc * SynAttributes * range
- Attributes are not part of the range of an SynModuleDecl, consider custom range?
- Tests without newline in original source
- StrictMode
*)
