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

It prints a table of every layout against four checks that can be automated: the output parses,
formatting it again returns the same text, an infix `%` has not silently become a quotation splice,
and renaming the left-hand side moves nothing. It also counts lines whose indentation is not a
multiple of the indent size, which is what separates the last two candidates from the rest.

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
