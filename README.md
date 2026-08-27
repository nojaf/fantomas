Fantomas
========

![Fantomas logo](https://raw.githubusercontent.com/fsprojects/fantomas/main/fantomas_logo.png)

![GitHub Workflow Status (event)](https://img.shields.io/github/actions/workflow/status/fsprojects/fantomas/main.yml?branch=main&label=Build%20main&style=flat-square)
[![Discord](https://img.shields.io/discord/196693847965696000?label=F%23%20Discord&style=flat-square)](https://discord.com/channels/196693847965696000/1493226271767924747)
[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/fantomas?style=flat-square)](https://www.nuget.org/packages/fantomas/absoluteLatest)
[![llms.txt](https://img.shields.io/badge/llms.txt-338cbb?style=flat-square)](https://fsprojects.github.io/fantomas/llms.txt)
[![llms-full.txt](https://img.shields.io/badge/llms--full.txt-338cbb?style=flat-square)](https://fsprojects.github.io/fantomas/llms-full.txt)

An [**opinionated**](https://fsprojects.github.io/fantomas/docs/end-users/StyleGuide.html) F# source code formatter.                  

> dotnet tool install fantomas

Documentation is available at https://fsprojects.github.io/fantomas/docs/index.html

If you point a coding agent at Fantomas, give it [llms.txt](https://fsprojects.github.io/fantomas/llms.txt) for an index of the documentation, or [llms-full.txt](https://fsprojects.github.io/fantomas/llms-full.txt) for all of it in one file.

## This branch: an experiment in infix layout

This branch is not a change to Fantomas. It is the test bench behind a style-guide discussion,
published so that the claims made there can be checked rather than taken on trust.

**The question.** When an infix expression with `=`, `>`, `<`, `%` or `%%` is too long for one line,
where does the right-hand side go? Fantomas has answered it three different ways over the years and
none of them is written down in either F# style guide.

**The note** is an issue on [fsharp/fslang-design](https://github.com/fsharp/fslang-design#style-guide),
which is where F# style is decided. It sets out what the parser rules out, what a good layout has to
achieve, the candidate layouts with the same examples under each, and what is proposed. This branch
is what it was written from, so that every sample in it can be reproduced.

**The bench** is `infix-lab/`. Forty examples, each formatted under all ten layouts, twice over with
two different names on the left so that a layout which places the right-hand side relative to the
name is caught rather than looked for by hand.

### Running it

The lab needs a debug build first, and then runs on its own:

```
dotnet build src/Fantomas/Fantomas.fsproj
./scripts/infix-lab.fsx
```

It prints a table of every layout against four checks that can be automated:

- **invalid**: the output is parsed back, so a layout that emits code the compiler rejects is caught
  rather than admired.
- **not idempotent**: the output is formatted a second time and compared. A layout that does not
  settle is unusable whatever it looks like.
- **meaning changed**: inside a quotation, an infix `%` at the wrong column is read as a splice. The
  Oak of the output is checked so that this cannot slip through as merely ugly.
- **shifted by a rename**: each example carries `LHS` where the name on the left goes, and is
  formatted twice, with `xs` and with `xsy`. A layout that places the right-hand side relative to
  that name shifts by exactly that one column.

The two names differ by a single character deliberately. A longer name would push examples over the
page width, and the reflow that follows would hide the shift being measured. Where even one
character crosses the width, the example is reported as inconclusive rather than counted either way;
the same three examples are inconclusive under every layout, which is what a page-width effect looks
like.

It also counts lines whose indentation is not a multiple of the indent size. A construct placed
under its own opening token lands wherever that token happens to sit, which is a column nobody chose
and which moves when anything before it changes width. Two of those survive under the best layouts,
both a tuple placing its second element under its own opening parenthesis, which every layout does
because the tuple does it.

One thing the table does not cover: every example is formatted at the default bracket style, so the
Stroustrup behaviour is not exercised. Under `aligned` it changes nothing, and the two layouts that
differ only in it come out identical. Those examples were produced by hand with the setting turned
on and checked the same way, but they are not in the counts.

It writes `infix-lab/SUMMARY.md` with the results in full, and one folder per layout under
`infix-lab/output/`, so two layouts can be compared directly:

```
diff -r infix-lab/output/beside_bracket_aware infix-lab/output/hybrid_stroustrup
```

### Trying a layout on your own code

Every layout sits behind an experimental setting. It defaults to `beside_bracket_aware`, which is
what Fantomas does today, so that checking out the branch changes nothing. That is the only reason
it is the default: it is one of the layouts the note rejects, and it is not consistent with itself
either, since its bracket carve-out reaches lists and arrays and stops there.

```
dotnet fsi scripts/format.fsx --editorconfig "fsharp_experimental_infix_layout=hybrid_stroustrup" YourFile.fs
```

The accepted values are `beside`, `beside_indented`, `beside_bracket_aware`, `own_line_when_needed`,
`own_line_always`, `operator_next_line`, `operator_next_line_own`, `operator_next_line_deep`,
`hybrid` and `hybrid_stroustrup`. The note explains each of them, in that order.

None of this is intended to ship as a setting. It exists so the options can be compared against real
input before one of them is argued for.

## Contributing Guidelines

See the [Contribution Guidelines](./CONTRIBUTING.md) and our [contributors documentation](https://fsprojects.github.io/fantomas/docs/contributors/Index.html)
