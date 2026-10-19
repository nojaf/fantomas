# Better patterns: decisions

What `fsharp_experimental_better_patterns` does, and why, and what is still open. The flag is
temporary: once the layout settles it becomes the default and goes away.

About the examples:

- They are formatted at `max_line_length = 60`, with the default settings otherwise
  (`fsharp_multiline_bracket_style = aligned`, `indent_size = 4`) unless they say different.
- 🟦 is the default: the flag off, real output of the local build.
- ✅ is the flag on. In section 1 it shows what was decided, with every decision applied, even when
  the code does not do it yet; each decision says whether it is implemented. In section 2 it is
  real output of the local build, labelled "today".
- ❌ does not parse, with the error.
- **Expression** is what the local build does with the same code written as an expression.
- **Option** is written by hand, and parses.

## 1. Decisions made

### 1.1 Follow expressions wherever the parser allows

Implemented for what section 1 covers.

The style guide says "Pattern matching formatting should be consistent with expression formatting".
When a pattern has an expression counterpart, it takes that layout. It only deviates where the
parser rejects the expression layout, and each deviation below names the error it avoids.

Expression:

```fsharp
let x =
    SynExpr.App(
        isInfix = true,
        funcExpr =
            SynExpr.LongIdent(
                longDotId = longDotId,
                extra = extra
            ),
        argExpr = argExpr
    )
```

✅ The same shape as a pattern:

```fsharp
match e with
| SynExpr.App(
    isInfix = true
    funcExpr =
        SynExpr.LongIdent(
            longDotId = longDotId
            extra = extra
        )
    argExpr = argExpr
  ) ->
    body
| _ -> ()
```

### 1.2 Respect `fsharp_multiline_bracket_style`

Implemented for what section 1 covers.

Cramped, aligned and Stroustrup each apply to patterns as they do to expressions. They start to
differ with records and lists, whose layout is still open (2.4). For named fields the three give the
same output.

✅ Cramped, aligned and Stroustrup alike:

```fsharp
match e with
| SynExpr.App(
    isInfix = true
    funcExpr =
        SynExpr.LongIdent(
            longDotId = longDotId
            extra = extra
        )
    argExpr = argExpr
  ) ->
    body
| _ -> ()
```

### 1.3 The closer of named fields goes on a line of its own

Implemented.

A named argument list in an expression closes this way. The old layout put `)` behind the last
field (#2670).

🟦

```fsharp
match e with
| SynExpr.App(
    isInfix = true; funcExpr = funcExpr; argExprXY = arg) ->
    body
| _ -> ()
```

✅

```fsharp
match e with
| SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExprXY = arg
  ) ->
    body
| _ -> ()
```

### 1.4 A closer goes back to the start of the line its parenthesis opened on

Implemented.

This is the expression rule. In a clause there is one limit: the closer of the clause pattern on
the column of the bar (or left of it) ends the clause. So in a clause that closer never goes left of
the column after `| `. A closer inside parentheses is not limited: the open parenthesis keeps the
clause going.

✅ In a clause, the closer of the pattern goes under the start of the pattern (bar + 2):

```fsharp
match e with
| SynExpr.App(
    isInfix = true
    argExprWithALongName = argExpr
  ) ->
    body
| _ -> ()
```

❌ The closer on the column of the bar, as the expression rule alone would put it:

```fsharp
match e with
| SynExpr.App(
    isInfix = true
    argExprWithALongName = argExpr
) ->
    body
| _ -> ()
```

```
error FS0010: Incomplete structured construct at or before this point in pattern matching.
Expected '->' or other token.
```

✅ Outside a clause (`let`, `let!`, `fun`, `member`, function parameters) there is no such limit,
and the closer goes back to the start of its line:

```fsharp
let f
    (SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExprWithALongName = argExpr
    ))
    =
    body
```

### 1.5 Every bar of an or-pattern is treated like the bar of the clause

Implemented.

In the syntax tree the first `| ` belongs to the clause, and only the ones after it belong to the
or-pattern. For the layout they are the same: each alternative starts on the column after its own
`| `, and none of its lines go left of that column. So the closer of each alternative lands under
the start of that alternative, two columns right of its bar. Before, only the first alternative had
this. The closer of the second one landed on its bar, and a token on the column of a bar ends the
clause, so the result did not parse.

🟦

```fsharp
match e with
| SynExpr.App(
    isInfix = true; argExpr = argExpr; extra = extraArgument)
| SynExpr.Lambda(
    fromMethod = fromMethod; body = lambdaBody; seq = seq) ->
    body
| _ -> ()
```

✅

```fsharp
match e with
| SynExpr.App( // the bar of the clause: the first alternative starts after it
    isInfix = true
    argExpr = argExpr
    extra = extraArgument
  ) // back on the column the first alternative starts on
| SynExpr.Lambda( // a bar of the or-pattern: the second alternative starts after it
    fromMethod = fromMethod
    body = lambdaBody
    seq = seq
  ) -> // back on the column the second alternative starts on
    body
| _ -> ()
```

### 1.6 A single argument in parentheses breaks after its parenthesis

Implemented.

`Some(…)` breaks like the argument of an application. A pattern in parentheses on its own, `(…)`,
keeps hugging them, as a parenthesised expression does, and its closers go back to the start of the
line, as in an expression. Whether that stays is open (2.13). A tuple in parentheses puts one item
per line.

🟦

```fsharp
match e with
| Some(SynExpr.App(
    isInfix = true; funcExpr = funcExpr; argExpr = argExpr)) ->
    body
| (SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExprLonger = argExpr)) -> body
| _ -> ()
```

✅

```fsharp
match e with
| Some(
    SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExpr = argExpr
    )
  ) ->
    body
| (SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExprLonger = argExpr
)) ->
    body
| _ -> ()
```

### 1.7 A named field that does not fit behind its `=` goes on the next line

Implemented.

The value of a named argument does this in an expression, and on that line it gets the whole width
first. A record pattern cannot break after its `=` (the parser rejects it), so record fields keep
the old layout.

🟦

```fsharp
match e with
| SynExpr.App(
    isInfix = true
    funcExpr = SynExpr.LongIdent(
        longDotId = longDotId; extra = extra)
    argExpr = argExpr) -> body
| _ -> ()
```

✅

```fsharp
match e with
| SynExpr.App(
    isInfix = true
    funcExpr =
        SynExpr.LongIdent(
            longDotId = longDotId
            extra = extra
        )
    argExpr = argExpr
  ) ->
    body
| _ -> ()
```

### 1.8 With an indent of 2, what is inside the parentheses goes right of its closer

Implemented.

In a clause the closer goes two columns right of the bar, after `| `. With an indent of 2, one
indent level in from the bar is that same column, so what is inside the parentheses would line up
with the closer and with the start of the pattern, and nothing would show it is inside. So it goes
one level further, bar + 4. This holds for named fields and for a single argument in parentheses.
The closer and the body share bar + 2, as any body after `->` does at that indent.

🟦 `indent_size = 2`:

```fsharp
match e with
| SynExpr.App(
    isInfix = true
    funcExpr = SynExpr.LongIdent(
      longDotId = longDotId; extra = extra)
    argExpr = argExpr) -> body
| Some(SynExpr.App(
    isInfix = true; funcExpr = funcExpr; argExpr = argExpr)) ->
  body
| _ -> ()
```

✅ `indent_size = 2`:

```fsharp
match e with
| SynExpr.App(
    isInfix = true
    funcExpr =
      SynExpr.LongIdent(
        longDotId = longDotId
        extra = extra
      )
    argExpr = argExpr
  ) ->
  body
| Some(
    SynExpr.App(
      isInfix = true
      funcExpr = funcExpr
      argExpr = argExpr
    )
  ) ->
  body
| _ -> ()
```

### 1.9 A multiline pattern puts the body on a line of its own

Implemented for a plain clause and an or-pattern. A clause with a `when` guard (2.12), a
Stroustrup body (2.2), comments before its arrow (3.1), or kept indentation in the last branch (3.2)
keeps the old clause layout for now; only its pattern is new.

**When a pattern takes more than one line, the body goes on the next line, one indent level in from
the bar, however short it is.** A body that fits behind the arrow does not stay there. A binding
does the same after a multiline signature. Where a `when` guard goes is open (2.12).

🟦

```fsharp
match e with
| SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExprLonger = argExpr) -> body
| _ -> ()
```

✅

```fsharp
match e with
| SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExprLonger = argExpr
  ) ->
    body
| _ -> ()
```

### 1.10 An or-pattern on several lines is a multiline pattern

Implemented.

As after any multiline pattern, the body goes on the next line, one indent level in from the bar,
however short it is. Behind the last alternative it reads as belonging to that alternative alone.
At the top of a clause, an or-pattern always takes more than one line (1.12), so every clause with
one moves its body. A rough count finds about 136 such clauses in our own code.

🟦

```fsharp
match e with
| SomeVeryLongUnionCaseNameNumberOne
| SomeVeryLongUnionCaseNameNumberTwo -> body
| _ -> ()
```

✅

```fsharp
match e with
| SomeVeryLongUnionCaseNameNumberOne
| SomeVeryLongUnionCaseNameNumberTwo ->
    body
| _ -> ()
```

### 1.11 `fsharp_align_function_signature_to_indentation` decides where the arrow goes

Implemented for what 1.9 covers.

After a multiline pattern, an or-pattern on several lines included, the body is on a line of its
own whatever this setting says. The setting only decides the arrow.

✅ Setting off (the default): the arrow stays behind the pattern.

```fsharp
match e with
| SynExpr.App(
    isInfix = true
    argExprWithALongName = argExpr
  ) ->
    body
| SomeVeryLongUnionCaseNameNumberOne
| SomeVeryLongUnionCaseNameNumberTwo ->
    body
| _ -> ()
```

✅ Setting on: the arrow gets a line of its own, one indent level in from the bar, on the column of
the body.

```fsharp
match e with
| SynExpr.App(
    isInfix = true
    argExprWithALongName = argExpr
  )
    ->
    body
| SomeVeryLongUnionCaseNameNumberOne
| SomeVeryLongUnionCaseNameNumberTwo
    ->
    body
| _ -> ()
```

This is what the setting does to the `=` of a `let` with a return type: there too, the `=` moves to
a line of its own on the column of the body. 🟦 Setting off:

```fsharp
let someFunctionName
    (firstParameter: int)
    (secondParameter: int)
    (thirdParameter: int)
    : int =
    body
```

🟦 Setting on:

```fsharp
let someFunctionName
    (firstParameter: int)
    (secondParameter: int)
    (thirdParameter: int)
    : int
    =
    body
```

A multiline `when` guard puts `->` on that column today too. For members,
`fsharp_alternative_long_member_definitions` does what this setting does for `let`.

### 1.12 At the top of a clause, every alternative of an or-pattern gets a line of its own

Already what the old layout does, so nothing to implement. However short the alternatives are,
there is no check whether they fit on one line. An or-pattern nested in parentheses, a list or a
record stays on one line when it fits (#1252).

In a long match, two short alternatives squeezed onto one line are easy to miss: the reader counts
the bars down the left, and `| A | B` looks like one case where there are two. Neither style guide
mentions or-patterns.

Considered and left alone: letting it depend on the other clauses, so that `| A | B -> x` followed
only by `| _ -> y` stays on one line. That makes one clause's layout depend on its neighbours, which
nothing else in a match does.

🟦

```fsharp
match x with
| A
| B -> body
| Some(A | B) -> body
| _ -> ()
```

✅ With the body on a line of its own after an or-pattern on several lines (1.10):

```fsharp
match x with
| A
| B ->
    body
| Some(A | B) -> body
| _ -> ()
```

### 1.13 A tuple in parentheses puts one item per line

Implemented.

A tuple in the parentheses of a case, `Case(a, b)`, that does not fit on the rest of the line breaks
after its opening parenthesis, puts one item per line, one indent level in, and closes on a line of
its own, as the arguments of a method call do. In a clause the closer goes under the start of the
pattern, two columns right of the bar. A tuple that fits stays on one line.

🟦

```fsharp
match e with
| SynExpr.App(ExprAtomicFlag.NonAtomic,
              false,
              SynExpr.Ident funcIdent,
              argumentExpression,
              range) -> body
| GetSetMember(Some(SynBinding(
                   accessibility = getterAccessibility)),
               Some(setterBinding)) -> body
| Some(a, b) -> body
| _ -> ()
```

✅

```fsharp
match e with
| SynExpr.App(
    ExprAtomicFlag.NonAtomic,
    false,
    SynExpr.Ident funcIdent,
    argumentExpression,
    range
  ) ->
    body
| GetSetMember(
    Some(SynBinding(accessibility = getterAccessibility)),
    Some(setterBinding)
  ) ->
    body
| Some(a, b) -> body
| _ -> ()
```

### 1.14 A tuple at the top of a clause and curried arguments put one item per line

Implemented.

A tuple without parentheses at the top of a clause that does not fit puts one item per line, each
where the first one starts, two columns right of the bar, after `| `. Each item is laid out as a
pattern there: its closer goes back to that column, its content one indent level in from the bar.

Curried arguments that do not fit go on lines of their own, one indent level in, as the arguments of
an application do. With an indent of 2 they go one level further, so they stand right of the name.

With `fsharp_align_function_signature_to_indentation`, as in our own code:

🟦

```fsharp
match e with
| SynExpr.Ident firstIdentifier,
  SynExpr.Ident secondIdentifier,
  SynExpr.Ident thirdIdentifier -> body
| SynExpr.App(
    isInfix = true; funcExpr = funcExpr; argExpr = argExpr),
  SynExpr.Ident secondIdentifier -> body
| ParseArguments firstArgumentPattern secondArgumentPattern thirdArgumentPattern ->
    body
| a, b -> body
| _ -> ()
```

✅

```fsharp
match e with
| SynExpr.Ident firstIdentifier,
  SynExpr.Ident secondIdentifier,
  SynExpr.Ident thirdIdentifier
    ->
    body
| SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExpr = argExpr
  ),
  SynExpr.Ident secondIdentifier
    ->
    body
| ParseArguments
    firstArgumentPattern
    secondArgumentPattern
    thirdArgumentPattern
    ->
    body
| a, b -> body
| _ -> ()
```

### 1.15 A list at the top of a clause takes the layout of a list expression

Implemented.

A list or array at the top of a clause that does not fit puts its items on lines of their own, one
indent level in from the bar, and its closing bracket goes back to where the list starts, two
columns right of the bar. Stroustrup has nothing to hug there, and its closer cannot go back to the
bar, so it looks the same as aligned. With an indent of 2 the items go one level further, so they
stand right of the closing bracket.

Cramped keeps the items behind the opening bracket, as the cramped list expression does. A closer
inside an item goes under the start of that item.

What is inside an item is laid out as it would be anywhere else.

🟦 Every bracket style:

```fsharp
match e with
| [ SynBinding(
        headPat = SynPat.Named(ident = ident); expr = expr)
    SynBinding(
        headPat = SynPat.Named(ident = ident); expr = expr) ] ->
    body
| _ -> ()
```

✅ Aligned and Stroustrup:

```fsharp
match e with
| [
    SynBinding(
        headPat = SynPat.Named(ident = ident)
        expr = expr
    )
    SynBinding(
        headPat = SynPat.Named(ident = ident)
        expr = expr
    )
  ] ->
    body
| _ -> ()
```

✅ Cramped:

```fsharp
match e with
| [ SynBinding(
        headPat = SynPat.Named(ident = ident)
        expr = expr
    )
    SynBinding(
        headPat = SynPat.Named(ident = ident)
        expr = expr
    ) ] ->
    body
| _ -> ()
```

## 2. Decisions still to make

Some patterns ask two questions, and the answers may differ: a. how the pattern looks inside another
pattern, and b. how it looks as the pattern of a clause, right after `| `. There its own closer
cannot go back to the column of the bar, where it would end the clause.

### 2.1 Does the arrow keep borrowing `fsharp_align_function_signature_to_indentation`?

With the setting on, the arrow after a multiline pattern gets a line of its own, on the column of
the body:

```fsharp
match e with
| SynExpr.App(
    isInfix = true
    argExpr = argExpr
  )
    ->
    body
| _ -> ()
```

With it off, the arrow stays behind the closer (`) ->`) and the body goes on the next line.

The setting's name says nothing about patterns. The other way is a setting of its own for the
arrow.

### 2.2 Does a Stroustrup body hug the arrow after a multiline pattern?

A multiline pattern puts its body on a line of its own. A clause with a Stroustrup body keeps the
old clause layout for now, so its body still hugs the arrow.

✅ Stroustrup, today. The body hugs the arrow, as it does after a pattern on one line:

```fsharp
match e with
| SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExpr = argExpr
  ) -> {
    Name = funcExpression
    Argument = argumentExpression
    Range = range
  }
| _ -> ()
```

Option A, keep hugging (as above). A binding hugs `= {` after a multiline signature (🟦,
Stroustrup):

```fsharp
let someFunctionName
    (firstParameter: int)
    (secondParameter: int)
    (thirdParameter: int)
    = {
    A = firstParameter
    B = secondParameter
}
```

Option B, the body on a line of its own, as every other body after a multiline pattern:

```fsharp
match e with
| SynExpr.App(
    isInfix = true
    argExpr = argExpr
  ) ->
    {
        Name = funcExpression
        Argument = argumentExpression
        Range = range
    }
| _ -> ()
```

### 2.4 A list inside another pattern

A list inside another pattern still has its legacy layout under every bracket style: the items go
behind `[ `, and the closing bracket behind the last item. At the top of a clause a list already
takes the layout of a list expression.

✅ Today:

```fsharp
match e with
| SynModuleDecl.Let(
    bindings =
        [ SynBinding(
            headPat = SynPat.Named(ident = ident)
            expr = expr
          ) as binding ]
  ) ->
    body
| _ -> ()
```

The list as an expression, under each bracket style:

```fsharp
// aligned (the default)
bindings =
    [
        SynBinding(
            headPat = SynPat.Named(ident = ident),
            expr = expr
        )
    ]

// cramped
bindings =
    [ SynBinding(
          headPat = SynPat.Named(ident = ident),
          expr = expr
      ) ]

// stroustrup
bindings = [
    SynBinding(
        headPat = SynPat.Named(ident = ident),
        expr = expr
    )
]
```

Option, the pattern takes the layout of the expression. Aligned:

```fsharp
| SynModuleDecl.Let(
    bindings =
        [
            SynBinding(
                headPat = SynPat.Named(ident = ident)
                expr = expr
            ) as binding
        ]
  ) ->
    body
```

Cramped:

```fsharp
| SynModuleDecl.Let(
    bindings =
        [ SynBinding(
              headPat = SynPat.Named(ident = ident)
              expr = expr
          ) as binding ]
  ) ->
    body
```

Stroustrup, hugging the `=`:

```fsharp
| SynModuleDecl.Let(
    bindings = [
        SynBinding(
            headPat = SynPat.Named(ident = ident)
            expr = expr
        ) as binding
    ]
  ) ->
    body
```

### 2.5 Or-patterns inside another pattern

At the top of a clause every alternative of an or-pattern gets a line of its own, with its bar.
Anywhere else an or-pattern is inside another pattern: in parentheses (which is also how it appears
in a `let`, a lambda or a curried argument), in a named field, or as an item of a list. These are
one question, and one answer for all of them is the goal.

#### In parentheses and named fields

An or-pattern inside another pattern still has its legacy layout: the second alternative starts
behind the first, on the same line. A named field takes an or-pattern without parentheses. An
argument in parentheses, as after `Some`, has them anyway.

✅ Today, with a named field and without:

```fsharp
match e with
| SynExpr.App(
    funcExpr =
        SynExpr.Ident identifierOne | SynExpr.LongIdent(
            longDotId =
                SynLongIdent([ identifierOne ], _, _)
        )
  ) ->
    body
| Some(
    SynExpr.Ident identifierOne | SynExpr.LongIdent(
        longDotId = SynLongIdent([ identifierOne ], _, _)
    )
  ) ->
    body
| _ -> ()
```

Option A, every alternative on a line of its own, its bar under the first alternative, as the
alternatives of a clause line up under one another. In a named field this parses:

```fsharp
match e with
| SynExpr.App(
    funcExpr =
        SynExpr.Ident identifierOne
        | SynExpr.LongIdent(
            longDotId = SynLongIdent([ identifierOne ], _, _)
          )
  ) ->
    body
| _ -> ()
```

❌ In parentheses a bar under the first alternative does not parse:

```fsharp
match e with
| Some(
    SynExpr.Ident identifierOne
    | SynExpr.LongIdent(
        longDotId = SynLongIdent([ identifierOne ], _, _)
      )
  ) ->
    body
| _ -> ()
```

```
error FS0010: Incomplete structured construct at or before this point in pattern.
Expected ')' or other token.
```

Option B (to revisit), every bar two columns right of the first alternative. The other alternatives
then start a whole indent level in from the first, and what is nested inside them lays out from
there in whole indent levels again. This parses in a named field and in parentheses:

```fsharp
match e with
| SynExpr.App(
    funcExpr =
        SynExpr.Ident identifierOne
          | SynExpr.LongIdent(
                longDotId =
                    SynLongIdent(
                        [ identifierOne ],
                        _,
                        _
                    )
            )
  ) ->
    body
| Some(
    SynExpr.Ident identifierOne
      | SynExpr.LongIdent(
            longDotId =
                SynLongIdent(
                    [ identifierOne ],
                    _,
                    _
                )
        )
  ) ->
    body
| _ -> ()
```

To settle with B:

- In parentheses the bar has to be right of the first alternative. One column right parses too; two
  columns put the other alternatives on a whole indent level.
- The first alternative stands a level left of the others.
- Where the closer of `SynExpr.LongIdent(` goes. Above it goes under its own alternative, as an
  alternative after the bar of a clause does. Under the bar or under the first alternative parse
  too.

#### In a list

🟦 A list of one or-pattern puts each alternative on a line of its own, its bar one column right of
the item. An or-pattern next to other items does the same, and the result does not parse:

```fsharp
match e with
| [ SynExpr.Ident firstIdentifier
     | SynExpr.Ident secondIdentifier
     | SynExpr.Ident thirdIdentifier ] -> body
| [ SynExpr.Ident firstIdentifier
    | SynExpr.Ident secondIdentifier
    SynExpr.Ident thirdIdentifier ] -> body
| _ -> ()
```

```
(6,4) Unexpected symbol '|' in pattern. Expected ']' or other token.
```

✅ Today, cramped. A list of one or-pattern keeps the layout above. Next to other items, the
alternatives stay on one line, and a name splits from its argument when they do not fit:

```fsharp
match e with
| [ SynExpr.Ident firstIdentifier
     | SynExpr.Ident secondIdentifier
     | SynExpr.Ident thirdIdentifier ] ->
    body
| [ SynExpr.Ident firstIdentifier | SynExpr.Ident
        secondIdentifier
    SynExpr.Ident thirdIdentifier ] ->
    body
| _ -> ()
```

✅ Today, aligned and Stroustrup. The alternatives stay on one line in both lists:

```fsharp
match e with
| [
    SynExpr.Ident firstIdentifier | SynExpr.Ident
        secondIdentifier | SynExpr.Ident thirdIdentifier
  ] ->
    body
| [
    SynExpr.Ident firstIdentifier | SynExpr.Ident
        secondIdentifier
    SynExpr.Ident thirdIdentifier
  ] ->
    body
| _ -> ()
```

### 2.6 `::` chains

Not covered by the flag yet. With the flag on, a `::` pattern comes out as the text `todo 2.6`, so
the gap cannot be missed. In our own code that is 93 places in 18 files.

#### 2.6a Inside another pattern

🟦

```fsharp
match e with
| Some(SynExpr.App(
    isInfix = true; funcExpr = funcExpr; argExpr = argExpr) :: rest) ->
    body
| _ -> ()
```

Expression:

```fsharp
let v =
    Some(
        SynExpr.App(
            isInfix = true,
            funcExpr = funcExpr,
            argExpr = argExpr
        )
        :: rest
    )
```

Option A, keep `) ::` on the line of the closer:

```fsharp
match e with
| Some(
    SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExpr = argExpr
    ) :: rest
  ) ->
    body
| _ -> ()
```

Option B, every `::` at the start of a line, like the expression:

```fsharp
match e with
| Some(
    SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExpr = argExpr
    )
    :: rest
  ) ->
    body
| _ -> ()
```

#### 2.6b As the pattern of a clause

🟦

```fsharp
match e with
| SynExpr.App(
    isInfix = true; funcExpr = funcExpr; argExpr = argExpr) :: SynExpr.Lambda(
      fromMethod = fromMethod; body = lambdaBody) :: rest ->
    body
| _ -> ()
```

Expression:

```fsharp
let x =
    SynExpr.App(
        isInfix = true,
        funcExpr = funcExpr,
        argExpr = argExpr
    )
    :: SynExpr.Lambda(
        fromMethod = fromMethod,
        body = lambdaBody
    )
    :: rest
```

Option A, keep `) ::` on the line of the closer, with every item's content at bar + 4:

```fsharp
match e with
| SynExpr.App(
    isInfix = true
    argExpr = argExpr
  ) :: SynExpr.Lambda(
    fromMethod = fromMethod
    body = lambdaBody
  ) :: rest ->
    body
| _ -> ()
```

Option B, every `::` at the start of a line, like the expression. As in the expression, the
content of the second item is one level in from its `::`, and its closer goes back under the `::`:

```fsharp
match e with
| SynExpr.App(
    isInfix = true
    argExpr = argExpr
  )
  :: SynExpr.Lambda(
      fromMethod = fromMethod
      body = lambdaBody
  )
  :: rest ->
    body
| _ -> ()
```

### 2.7 When `pattern ->` does not fit

Today only the pattern is measured. Here it fits on the line, but the arrow behind it goes past the
width (63 columns of 60).

✅ Today:

```fsharp
match e with
| SynExpr.App(isInfix = true; funcExpr = f; argExpr = argEx) ->
    body
| _ -> ()
```

Option, the arrow counts towards the width, so the pattern takes its long form, and the body goes on
a line of its own as after any multiline pattern. `pattern-layout-walkthrough.md` proposes this as
its rule 6:

```fsharp
match e with
| SynExpr.App(
    isInfix = true
    funcExpr = f
    argExpr = argEx
  ) ->
    body
| _ -> ()
```

It measures every clause pattern a second time, which needs a benchmark.

Option, the pattern stays on one line and only the arrow moves: it goes on a line of its own, one
indent level in from the bar, on the column of the body. The pattern is not laid out again, only the
arrow is measured:

```fsharp
match e with
| SynExpr.App(isInfix = true; funcExpr = f; argExpr = argEx)
    ->
    body
| _ -> ()
```

Our own code has a clause like this in `ASTTransformer.fs`: the pattern ends on column 120 and the
arrow behind it reaches 123.

```fsharp
    | SynExpr.ObjExpr(t, eio, withKeyword, bd, members, ims, StartRange 3 (mNew, _), StartEndRange 1 (mOpen, _, mClose)) ->
```

### 2.8 The shape of a `let` binding with a multiline pattern

✅ Today. The pattern breaks behind the `let`:

```fsharp
let mutable (CaseName(
    firstArgumentPattern,
    secondArgumentPatternXYZ
)) =
    x

let (SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExpr = argExpr
    range = range
)) =
    x
```

`pattern-layout-walkthrough.md` (its section 6) proposes another shape. The pattern goes on a line
of its own, one level in, and only breaks when it does not fit there. The `=` and the body follow on that column, as they do
for a function with parameters today (✅):

```fsharp
let f
    (SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExprWithALongName = argExpr
    ))
    =
    body
```

Option:

```fsharp
let mutable
    (CaseName(firstArgumentPattern, secondArgumentPatternXYZ))
    =
    x

let
    (SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExpr = argExpr
        range = range
    ))
    =
    x
```

### 2.9 `try/with` without a bar

The only clause of a `with` can go without a bar. With the flag on, Fantomas cannot format such a
clause when its pattern takes more than one line: it lays the pattern out as if a bar came before
it, so the closer lands left of the pattern, and the result does not parse. Fantomas then reports a
bug. This needs fixing whatever the layout becomes.

🟦

```fsharp
try
    body
with SynExpr.App(
         isInfix = true
         funcExpr = funcExpr
         argExpr = argExpr) ->
    handler
```

❌ Today, what the flag writes:

```fsharp
try
    body
with SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExpr = argExpr
  ) ->
    handler
```

```
error FS0010: Incomplete structured construct at or before this point in pattern matching.
Expected '->' or other token.
```

Option, add the bar when the pattern is multiline. The clause then looks like any other clause with
a multiline pattern:

```fsharp
try
    body
with
| SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExpr = argExpr
  ) ->
    handler
```

Option, keep the content lined up after `with `, and its closer under the start of the pattern, as
before the flag laid out clauses from their bar:

```fsharp
try
    body
with SynExpr.App(
         isInfix = true
         funcExpr = funcExpr
         argExpr = argExpr
     ) ->
    handler
```

### 2.11 Update `pattern-layout-walkthrough.md`

Once the layout settles, so it describes what was built.

### 2.12 `when` guards

A guard sits between the pattern and the arrow, so it meets every decision about the pattern and the
body. Three situations: a short guard after a multiline pattern, a guard that does not fit after a
short pattern, and a guard that does not fit after a multiline pattern. Whatever the guard does, the
body already goes on a line of its own after a multiline pattern or a guard that breaks.

🟦

```fsharp
match e with
| SynExpr.App(
    isInfix = true; funcExpr = funcExpr; argExpr = argExpr) when
    isInfix
    ->
    body
| SynExpr.Ident ident when
    ident.idText.StartsWith("op_")
    && not (isOperatorWithoutSpace ident.idText)
    ->
    body
| SynExpr.App(
    isInfix = true; funcExpr = funcExpr; argExpr = argExpr) when
    funcExpr.Range.StartLine <> argExpr.Range.EndLine
    && isInfix
    ->
    body
| _ -> ()
```

✅ Today. A clause with a guard keeps the old clause layout; only its pattern is new. So a short
guard and a short body stay behind the closer. A guard that does not fit breaks after `when`, its
condition one level in from the bar, and the arrow gets a line of its own on the column of the body:

```fsharp
match e with
| SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExpr = argExpr
  ) when isInfix -> body
| SynExpr.Ident ident when
    ident.idText.StartsWith("op_")
    && not (isOperatorWithoutSpace ident.idText)
    ->
    body
| SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExpr = argExpr
  ) when
    funcExpr.Range.StartLine <> argExpr.Range.EndLine
    && isInfix
    ->
    body
| _ -> ()
```

Expression. The closest counterpart is the condition of an `if`. It goes on lines of its own one
level in, and `then` goes back to the column of `if`:

```fsharp
let x =
    if
        ident.idText.StartsWith("op_")
        && not (isOperatorWithoutSpace ident.idText)
    then
        body
    else
        other
```

Option A, what the flag does today (✅ above), except that the body after a multiline pattern goes
on a line of its own, as in any other clause:

```fsharp
match e with
| SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExpr = argExpr
  ) when isInfix ->
    body
| _ -> ()
```

Option B, as today, but the arrow after a guard that does not fit goes back under the start of the
pattern, as `then` goes back to the column of `if`. The arrow then separates the guard from the
body, and the body stands out one level further in:

```fsharp
match e with
| SynExpr.Ident ident when
    ident.idText.StartsWith("op_")
    && not (isOperatorWithoutSpace ident.idText)
  ->
    body
| SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExpr = argExpr
  ) when
    funcExpr.Range.StartLine <> argExpr.Range.EndLine
    && isInfix
  ->
    body
| _ -> ()
```

Option C, after a multiline pattern `when` starts a line of its own, one level in from the bar, and
a guard that does not fit goes one level further in. The arrow goes on the column of `when`:

```fsharp
match e with
| SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExpr = argExpr
  )
    when isInfix
    ->
    body
| SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExpr = argExpr
  )
    when
        funcExpr.Range.StartLine <> argExpr.Range.EndLine
        && isInfix
    ->
    body
| _ -> ()
```

Also open: with `fsharp_align_function_signature_to_indentation` on, the arrow after a multiline
pattern gets a line of its own. Whether a short guard then stays behind the closer (`) when
isInfix`) or moves with the arrow is part of this choice.

### 2.13 A pattern in parentheses on its own

#### 2.13a Inside another pattern

The parentheses hug the pattern inside, as they do around an expression, and the closers go back
to the start of their line. Fine for now. Curried arguments, as an example:

✅ Today, with `fsharp_align_function_signature_to_indentation`:

```fsharp
match e with
| ParseArguments
    (Some(
        SynExpr.App(
            ExprAtomicFlag.NonAtomic,
            false,
            funcExpr,
            argExpr,
            range
        )
    ))
    secondArgumentPattern
    ->
    body
| _ -> ()
```

Expression:

```fsharp
let v =
    ParseArguments
        (Some(
            SynExpr.App(
                ExprAtomicFlag.NonAtomic,
                false,
                funcExpr,
                argExpr,
                range
            )
        ))
        secondArgumentPattern
```

#### 2.13b As the pattern of a clause

A clause pattern only needs parentheses around it for a type annotation: `| x: int -> x` does not
parse (the type takes `-> x` as a function type), `| (x: int) -> x` does. Anywhere else they can go.
A multiline pattern that starts with them is a bit of a smell in the code, so a layout that makes
them stand out is fine. Fantomas keeps them either way: it never removes parentheses.

Which closer ends a clause: only the closer of the clause pattern itself. A closer inside
parentheses may sit on the column of the bar, because the open parenthesis keeps the clause going.

🟦 Default:

```fsharp
match e with
| (SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExprLonger = argExpr)) -> body
| _ -> ()
```

✅ Today, as the expression. The inner closer sits on the column of the bar, the outer one right of
it:

```fsharp
match e with
| (SynExpr.App(
    isInfix = true
    funcExpr = funcExpr
    argExprLonger = argExpr
)) ->
    body
| _ -> ()
```

**Expression**, the parentheses stay together and the closers go back to the start of the line:

```fsharp
let v =
    (SynExpr.App(
        isInfix = true,
        argExprLongerThanThis = argExprAndThisAsWell,
        funcExpr = funcExpr,
        range = range,
        more = moreAndMoreAndMore
    ))
```

Option, as `Some(…)`: the parenthesis breaks after `(`, the pattern inside goes one indent level in,
and every closer gets a line of its own. The parentheses are hard to miss:

```fsharp
match e with
| (
    SynExpr.App(
        isInfix = true
        funcExpr = funcExpr
        argExprLonger = argExpr
    )
  ) ->
    body
| _ -> ()
```

### 2.14 Records

#### 2.14a Inside another pattern

✅ Today, aligned and Stroustrup. A record that does not fit goes on the next line, one indent level
in, as an argument or the value of a named field does, and puts its fields on lines of their own:

```fsharp
match e with
| Some
    {
        FieldName = fieldName
        Expr = Some fieldExpression
        Range = range
    } ->
    body
| SynExpr.Record(
    copyInfo =
        {
            FieldName = fieldName
            Expr = Some fieldExpression
            Range = range
        }
  ) ->
    body
| _ -> ()
```

✅ Today, cramped. The same, with the fields behind `{ `:

```fsharp
match e with
| Some
    { FieldName = fieldName
      Expr = Some fieldExpression
      Range = range } ->
    body
| SynExpr.Record(
    copyInfo =
        { FieldName = fieldName
          Expr = Some fieldExpression
          Range = range }
  ) ->
    body
| _ -> ()
```

The record as an expression. Aligned and cramped look as the pattern does today. Stroustrup hugs
`Some {` and `= {`:

```fsharp
let x =
    Some {
        FieldName = fieldName
        Expr = Some fieldExpression
        Range = range
    }

let y =
    SynExpr.Record(
        copyInfo = {
            FieldName = fieldName
            Expr = Some fieldExpression
            Range = range
        }
    )
```

Option, Stroustrup hugs as the expression does. The closer after `Some {` goes back to where the
pattern starts, after `| `:

```fsharp
match e with
| Some {
    FieldName = fieldName
    Expr = Some fieldExpression
    Range = range
  } ->
    body
| SynExpr.Record(
    copyInfo = {
        FieldName = fieldName
        Expr = Some fieldExpression
        Range = range
    }
  ) ->
    body
| _ -> ()
```

#### 2.14b As the pattern of a clause

🟦 Aligned and Stroustrup. The fields line up after `{ `:

```fsharp
match e with
| {
      FieldName = fieldName
      Expr = Some fieldExpression
      Range = range
  } -> body
| {
      FieldName = SynLongIdent(
          id = [ fieldIdentifier ]
          dotRanges = []
          trivia = trivia)
      Range = range
  } -> body
| _ -> ()
```

✅ Today, aligned and Stroustrup. The fields still line up after `{ `:

```fsharp
match e with
| {
      FieldName = fieldName
      Expr = Some fieldExpression
      Range = range
  } ->
    body
| {
      FieldName = SynLongIdent(
          id = [ fieldIdentifier ]
          dotRanges = []
          trivia = trivia
      )
      Range = range
  } ->
    body
| _ -> ()
```

✅ Today, cramped. The fields stay behind `{ `, as in the cramped record expression, and a closer
inside a field goes under the start of that field:

```fsharp
match e with
| { FieldName = fieldName
    Expr = Some fieldExpression
    Range = range } ->
    body
| { FieldName = SynLongIdent(
        id = [ fieldIdentifier ]
        dotRanges = []
        trivia = trivia
    )
    Range = range } ->
    body
| _ -> ()
```

Option, aligned and Stroustrup take the layout a list at the top of a clause has: the fields go one
indent level in from the bar, and `}` goes back to where the record starts:

```fsharp
match e with
| {
    FieldName = fieldName
    Expr = Some fieldExpression
    Range = range
  } ->
    body
| {
    FieldName = SynLongIdent(
        id = [ fieldIdentifier ]
        dotRanges = []
        trivia = trivia
    )
    Range = range
  } ->
    body
| _ -> ()
```

### 2.15 `as` that does not fit

What comes after `as` is usually a name, but it can be a whole pattern. Two things can go wrong: the
name does not fit on the rest of the line, or the pattern after `as` goes over several lines.

#### 2.15a Inside another pattern

✅ Today. A name that does not fit stays on the line, past the maximum. A pattern after `as` keeps
`as` on the line, and its closer goes back to where the item starts:

```fsharp
match e with
| Some(
    SynExpr.Ident someLongIdentifierName as someEvenLongerAliasForTheWholeThing
  ) ->
    body
| Some(
    SynExpr.Ident identifier as SynExpr.App(
        ExprAtomicFlag.NonAtomic,
        false,
        funcExpr,
        argExpr,
        range
    )
  ) ->
    body
| _ -> ()
```

Option A, `as` stays on the line. A name that does not fit goes on the next line, one indent level
in:

```fsharp
match e with
| Some(
    SynExpr.Ident someLongIdentifierName as
        someEvenLongerAliasForTheWholeThing
  ) ->
    body
```

Option B, break before `as`, and put `as` one indent level in:

```fsharp
match e with
| Some(
    SynExpr.Ident someLongIdentifierName
        as someEvenLongerAliasForTheWholeThing
  ) ->
    body
| Some(
    SynExpr.Ident identifier
        as SynExpr.App(
            ExprAtomicFlag.NonAtomic,
            range
        )
  ) ->
    body
| _ -> ()
```

❌ In parentheses, `as` under the start of the pattern it follows does not parse, as a bar does not
(2.5):

```fsharp
match e with
| Some(
    SynExpr.Ident someLongIdentifierName
    as someEvenLongerAliasForTheWholeThing
  ) ->
    body
| _ -> ()
```

```
error FS0010: Incomplete structured construct at or before this point in pattern.
Expected ')' or other token.
```

#### 2.15b As the pattern of a clause

🟦 Every style. The pattern after `as` lines up behind its opening parenthesis:

```fsharp
match e with
| SynExpr.App(ExprAtomicFlag.NonAtomic,
              false,
              funcExpr,
              argExpr,
              range) as application -> body
| SynExpr.Ident identifier as SynExpr.App(ExprAtomicFlag.NonAtomic,
                                          false,
                                          funcExpr,
                                          argExpr,
                                          range) -> body
| SynExpr.Ident someLongIdentifierName as someEvenLongerAliasForTheWholeThing ->
    body
| _ -> ()
```

❌ Today. A name after a multiline pattern is fine, and a name that does not fit stays on the line,
past the maximum. But the closer of a pattern after `as` goes back to the start of its line, which
is the column of the bar. That ends the clause, and Fantomas reports that it cannot format the file:

```fsharp
match e with
| SynExpr.App(
    ExprAtomicFlag.NonAtomic,
    false,
    funcExpr,
    argExpr,
    range
  ) as application ->
    body
| SynExpr.Ident identifier as SynExpr.App(
    ExprAtomicFlag.NonAtomic,
    false,
    funcExpr,
    argExpr,
    range
) ->
    body
| SynExpr.Ident someLongIdentifierName as someEvenLongerAliasForTheWholeThing ->
    body
| _ -> ()
```

```
error FS0010: Incomplete structured construct at or before this point in pattern matching.
Expected '->' or other token.
```

Option A, `as` stays on the line. The closer of a pattern after `as` goes back to where the pattern
starts, after `| `, as other closers do. A name that does not fit goes on the next line, one indent
level in:

```fsharp
match e with
| SynExpr.Ident identifier as SynExpr.App(
    ExprAtomicFlag.NonAtomic,
    range
  ) ->
    body
| SynExpr.Ident someLongIdentifierName as
    someEvenLongerAliasForTheWholeThing ->
    body
| _ -> ()
```

Option B, break before `as`, and put `as` where the pattern starts, after `| `. Here that parses,
unlike in parentheses (2.15a):

```fsharp
match e with
| SynExpr.Ident identifier
  as SynExpr.App(
      ExprAtomicFlag.NonAtomic,
      range
  ) ->
    body
| SynExpr.App(
    ExprAtomicFlag.NonAtomic,
    range
  )
  as application ->
    body
| SynExpr.Ident someLongIdentifierName
  as someEvenLongerAliasForTheWholeThing ->
    body
| _ -> ()
```

## 3. Edge cases the old code handles

`genClauseLegacy` handles situations that we have not discussed for the flag yet. With the flag on,
a clause in one of these situations is handed to `genClauseLegacy` as a whole, so it keeps its old
layout (only its pattern is new). Each one needs a decision before the flag can lay it out.

### 3.1 Trivia before the arrow (#2888)

Left to the old clause layout.

Comments between the pattern and the arrow put the arrow on a line of its own. The old code writes
a space before it, so it does not land on the column of the bar.

🟦

```fsharp
match subcategory with
| BuildPhaseSubcategory.Internal
// Getting here means the compiler has ICE-d.
// Just treat as an unknown-to-LanguageService error.
 -> false
```

✅ Flag on, today: the same.

❌ Without the space, the arrow lands on the column of the bar, which ends the clause. Fantomas then
reports that it cannot format the file:

```fsharp
match subcategory with
| BuildPhaseSubcategory.Internal
// Getting here means the compiler has ICE-d.
// Just treat as an unknown-to-LanguageService error.
-> false
```

### 3.2 `fsharp_experimental_keep_indent_in_branch`

Left to the old clause layout. Our own code uses this setting.

With this setting, the body of the last clause stays on the indentation of the bar, after an empty
line, when it does not fit behind the arrow.

🟦 With the setting on:

```fsharp
let f x =
    match x with
    | None -> ()
    | Some x ->

    let y = x + 1
    printfn "%d" y
    y
```

✅ Flag on, today: the same. Without the setting, the body is indented as in any other clause:

```fsharp
let f x =
    match x with
    | None -> ()
    | Some x ->
        let y = x + 1
        printfn "%d" y
        y
```

### 3.3 A Stroustrup body keeps its closer off the bar (#2990)

Left to the old clause layout, as is every Stroustrup body (2.2). The old code takes an absolute
column for it.

A body that hugs the arrow closes its bracket back on the column its line starts on, which is the
column of the bar. There the bar of the next clause no longer parses. So the column after `| ` is
taken as the start column, and the closer lands under the pattern.

🟦 and ✅, Stroustrup:

```fsharp
match x with
| A -> {
    Name = funcExpression
    Argument = argumentExpression
    Range = range
  }
| _ -> ()
```

❌ The closer on the column of the bar:

```fsharp
match x with
| A -> {
    Name = funcExpression
    Argument = argumentExpression
}
| _ -> ()
```

```
error FS0010: Unexpected symbol '|' in implementation file
```
