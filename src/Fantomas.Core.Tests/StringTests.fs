module Fantomas.Core.Tests.StringTests

open NUnit.Framework
open FsUnit
open Fantomas.Core.Tests.TestHelpers

[<Test>]
let ``triple-quoted strings`` () =
    formatSourceString
        "let xmlFragment2 = \"\"\"<book author=\"Milton, John\" title=\"Paradise Lost\">\"\"\""
        { config with
            MaxValueBindingWidth = 80 }
    |> should
        equal
        "let xmlFragment2 = \"\"\"<book author=\"Milton, John\" title=\"Paradise Lost\">\"\"\"
"

[<Test>]
let ``string literals`` () =
    formatSourceString
        """
let xmlFragment1 = @"<book author=""Milton, John"" title=""Paradise Lost"">"
let str1 = "abc"
    """
        { config with
            MaxValueBindingWidth = 60 }
    |> prepend newline
    |> should
        equal
        """
let xmlFragment1 =
    @"<book author=""Milton, John"" title=""Paradise Lost"">"

let str1 = "abc"
"""

[<Test>]
let ``multiline strings`` () =
    formatSourceString
        """
let alu =
        "GGCCGGGCGCGGTGGCTCACGCCTGTAATCCCAGCACTTTGG\
        GAGGCCGAGGCGGGCGGATCACCTGAGGTCAGGAGTTCGAGA\
        CCAGCCTGGCCAACATGGTGAAACCCCGTCTCTACTAAAAAT\
        ACAAAAATTAGCCGGGCGTGGTGGCGCGCGCCTGTAATCCCA\
        GCTACTCGGGAGGCTGAGGCAGGAGAATCGCTTGAACCCGGG\
        AGGCGGAGGTTGCAGTGAGCCGAGATCGCGCCACTGCACTCC\
  AGCCTGGGCGACAGAGCGAGACTCCGTCTCAAAAA"B
    """
        config
    |> prepend newline
    |> should
        equal
        """
let alu =
    "GGCCGGGCGCGGTGGCTCACGCCTGTAATCCCAGCACTTTGG\
        GAGGCCGAGGCGGGCGGATCACCTGAGGTCAGGAGTTCGAGA\
        CCAGCCTGGCCAACATGGTGAAACCCCGTCTCTACTAAAAAT\
        ACAAAAATTAGCCGGGCGTGGTGGCGCGCGCCTGTAATCCCA\
        GCTACTCGGGAGGCTGAGGCAGGAGAATCGCTTGAACCCGGG\
        AGGCGGAGGTTGCAGTGAGCCGAGATCGCGCCACTGCACTCC\
  AGCCTGGGCGACAGAGCGAGACTCCGTCTCAAAAA"B
"""

[<Test>]
let ``multiline string piped`` () =
    formatSourceString
        """
let f a b =
    a "
" |> b
    """
        config
    |> prepend newline
    |> should
        equal
        """
let f a b =
    a
        "
"
    |> b
"""

[<Test>]
let ``preserve uncommon literals`` () =
    formatSourceString
        """
let a = 0xFFy
let c = 0b0111101us
let d = 0o0777
let e = 1.40e10f
let f = 23.4M
let g = '\n'
    """
        config
    |> prepend newline
    |> should
        equal
        """
let a = 0xFFy
let c = 0b0111101us
let d = 0o0777
let e = 1.40e10f
let f = 23.4M
let g = '\n'
"""

[<Test>]
let ``uncommon literals strict mode`` () =
    formatAST
        false
        """
let a = 0xFFy
let c = 0b0111101us
let d = 0o0777
let e = 1.40e10f
let f = 23.4M
let g = '\n'
    """
        config
    |> prepend newline
    |> should
        equal
        """
let a = -1y
let c = 61us
let d = 511
let e = 1.4e+10f
let f = 23.4M
let g = '\n'
"""

[<Test>]
let ``should preserve triple-quote strings`` () =
    formatSourceString
        "
    type GetList() =
        let switchvox_users_voicemail_getList_response = \"\"\"
            </response>\"\"\"

        let switchvox_users_voicemail_getList = \"\"\"
            </request>\"\"\"

        member self.X = switchvox_users_voicemail_getList_response"
        { config with
            MaxValueBindingWidth = 120 }
    |> prepend newline
    |> should
        equal
        "
type GetList() =
    let switchvox_users_voicemail_getList_response =
        \"\"\"
            </response>\"\"\"

    let switchvox_users_voicemail_getList =
        \"\"\"
            </request>\"\"\"

    member self.X = switchvox_users_voicemail_getList_response
"

[<Test>]
let ``should keep triple-quote strings`` () =
    formatSourceString
        "
[<EntryPoint>]
let main argv =
    use fun1 = R.eval(R.parse(text = \"\"\"
    function(i) {
        x <- rnorm(1000)
        y <- rnorm(1000)
        m <- lm(y~x)
        m$coefficients[[2]]
    }
    \"\"\"))
    0
"
        config
    |> prepend newline
    |> should
        equal
        "
[<EntryPoint>]
let main argv =
    use fun1 =
        R.eval (
            R.parse (
                text =
                    \"\"\"
    function(i) {
        x <- rnorm(1000)
        y <- rnorm(1000)
        m <- lm(y~x)
        m$coefficients[[2]]
    }
    \"\"\"
            )
        )

    0
"

[<Test>]
let ``chars should be properly escaped`` () =
    formatSourceString """let private peskyChars = [| '"' ; '\t' ; ' ' ; '\\' |]""" config
    |> should
        equal
        """let private peskyChars = [| '"'; '\t'; ' '; '\\' |]
"""

[<Test>]
let ``quotes should be escaped in strict mode`` () =
    formatAST
        false
        """
    let formatter =
        // escape commas left in invalid entries
        sprintf "%i,\"%s\""
"""
        config
    |> should
        equal
        """let formatter = sprintf "%i,\"%s\""
"""

[<Test>]
let ``empty lines in multi-line string should be preserved, 577`` () =
    formatSourceString
        "
let x = \"\"\"some

content

with empty lines\"\"\"
"
        config
    |> prepend newline
    |> should
        equal
        "
let x =
    \"\"\"some

content

with empty lines\"\"\"
"

[<Test>]
let ``string with newline inside union case, 1056`` () =
    formatSourceString
        """
[<Test>]
let ``newline in string`` () =
    let source = "\"
\""

    let triviaNodes =
        tokenize [] source
        |> getTriviaFromTokens
        |> List.filter (fun { Item = item } -> match item with | StringContent("\"\n\"") -> true  | _ -> false)

    List.length triviaNodes == 1
"""
        config
    |> prepend newline
    |> should
        equal
        """
[<Test>]
let ``newline in string`` () =
    let source =
        "\"
\""

    let triviaNodes =
        tokenize [] source
        |> getTriviaFromTokens
        |> List.filter (fun { Item = item } ->
            match item with
            | StringContent("\"\n\"") -> true
            | _ -> false)

    List.length triviaNodes == 1
"""

[<Test>]
let ``double backslash in triple quote string`` () =
    formatSourceString
        "
let a = \"\"\"\\\\\"\"\"
"
        config
    |> prepend newline
    |> should
        equal
        "
let a = \"\"\"\\\\\"\"\"
"

[<Test>]
let ``single string with compiler define`` () =
    formatSourceString
        """
"#if FOO"
"""
        config
    |> prepend newline
    |> should
        equal
        """
"#if FOO"
"""

[<Test>]
let ``trailing spaces in string should be preserved, 1941`` () =
    formatSourceString
        "
let s = \"\"\"aaaa   
bbb\"\"\"
"
        config
    |> prepend newline
    |> should
        equal
        "
let s =
    \"\"\"aaaa   
bbb\"\"\"
"

[<Test>]
let ``unicode null character should be recognized as a trivia item, 2050`` () =
    formatSourceString "let s = \"\\000\"" config
    |> should equal "let s = \"\\000\"\n"

[<Test>]
let ``character quotes should be preserved, 3076`` () =
    formatAST false "let s = 'A'" config |> should equal "let s = 'A'\n"
