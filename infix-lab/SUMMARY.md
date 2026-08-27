# infix-lab

40 examples, 10 layouts, page width 80.

Each example is formatted twice, with `LHS` replaced by `xs` and by `xsy`.
The two names differ by one character. A layout that places the right-hand side relative to
the name on the left shifts by exactly that column and shows up as `shifted`.

`inconclusive` means the extra character crossed the page width, so the example broke
somewhere new and nothing can be read from its columns either way. The same examples are
inconclusive under every layout, which is what you would expect of a page-width effect.

| layout | invalid | not idempotent | meaning changed | shifted by rename | inconclusive | off-grid | lines |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `beside` | 1 | ok | ok | 17 | 3 | 74 | 238 |
| `beside_indented` | ok | ok | ok | 17 | 3 | 73 | 240 |
| `beside_bracket_aware` | ok | ok | ok | 17 | 3 | 73 | 240 |
| `own_line_when_needed` | ok | ok | ok | ok | 3 | 2 | 246 |
| `own_line_always` | ok | ok | ok | ok | 3 | 2 | 248 |
| `operator_next_line` | ok | ok | ok | ok | 3 | 73 | 273 |
| `operator_next_line_own` | ok | ok | ok | ok | 3 | 2 | 281 |
| `operator_next_line_deep` | ok | ok | ok | ok | 3 | 2 | 305 |
| `hybrid` | ok | ok | ok | ok | 3 | 2 | 247 |
| `hybrid_stroustrup` | ok | ok | ok | ok | 3 | 2 | 247 |

`off-grid` counts lines whose indentation is not a multiple of the indent size. Those columns
are chosen by whatever token happens to precede them rather than by the layout, and they move
when that token changes width. `lines` is the total line count, as a measure of verbosity.

## `beside`

- does not compile: `21-comment-before-rhs` (FS3156 Unexpected token '=' or incomplete expression)
- shifted by rename: `03-record`
- shifted by rename: `04-record-copy`
- shifted by rename: `05-object-expr`
- shifted by rename: `10-match`
- shifted by rename: `11-try-with`
- shifted by rename: `12-if-then-else`
- shifted by rename: `14-tuple`
- shifted by rename: `17-greater-than`
- shifted by rename: `18-nested-infix`
- shifted by rename: `25-op-lt-record`
- shifted by rename: `30-ctx-if-condition`
- shifted by rename: `32-ctx-when-guard`
- shifted by rename: `34-ctx-lambda-body`
- shifted by rename: `35-ctx-and-chain`
- shifted by rename: `36-ctx-member-body`
- shifted by rename: `40-probe-eq-match`
- shifted by rename: `41-probe-pctpct-match`

Inconclusive, the rename crossed the page width: 15-chain-both, 26-op-modulo, 31-ctx-while.

## `beside_indented`

- shifted by rename: `03-record`
- shifted by rename: `04-record-copy`
- shifted by rename: `05-object-expr`
- shifted by rename: `10-match`
- shifted by rename: `11-try-with`
- shifted by rename: `12-if-then-else`
- shifted by rename: `14-tuple`
- shifted by rename: `17-greater-than`
- shifted by rename: `18-nested-infix`
- shifted by rename: `25-op-lt-record`
- shifted by rename: `30-ctx-if-condition`
- shifted by rename: `32-ctx-when-guard`
- shifted by rename: `34-ctx-lambda-body`
- shifted by rename: `35-ctx-and-chain`
- shifted by rename: `36-ctx-member-body`
- shifted by rename: `40-probe-eq-match`
- shifted by rename: `41-probe-pctpct-match`

Inconclusive, the rename crossed the page width: 15-chain-both, 26-op-modulo, 31-ctx-while.

## `beside_bracket_aware`

- shifted by rename: `03-record`
- shifted by rename: `04-record-copy`
- shifted by rename: `05-object-expr`
- shifted by rename: `10-match`
- shifted by rename: `11-try-with`
- shifted by rename: `12-if-then-else`
- shifted by rename: `14-tuple`
- shifted by rename: `17-greater-than`
- shifted by rename: `18-nested-infix`
- shifted by rename: `25-op-lt-record`
- shifted by rename: `30-ctx-if-condition`
- shifted by rename: `32-ctx-when-guard`
- shifted by rename: `34-ctx-lambda-body`
- shifted by rename: `35-ctx-and-chain`
- shifted by rename: `36-ctx-member-body`
- shifted by rename: `40-probe-eq-match`
- shifted by rename: `41-probe-pctpct-match`

Inconclusive, the rename crossed the page width: 15-chain-both, 26-op-modulo, 31-ctx-while.

## `own_line_when_needed`

Nothing found by any of the checks.

Inconclusive, the rename crossed the page width: 15-chain-both, 26-op-modulo, 31-ctx-while.

## `own_line_always`

Nothing found by any of the checks.

Inconclusive, the rename crossed the page width: 15-chain-both, 26-op-modulo, 31-ctx-while.

## `operator_next_line`

Nothing found by any of the checks.

Inconclusive, the rename crossed the page width: 15-chain-both, 26-op-modulo, 31-ctx-while.

## `operator_next_line_own`

Nothing found by any of the checks.

Inconclusive, the rename crossed the page width: 15-chain-both, 26-op-modulo, 31-ctx-while.

## `operator_next_line_deep`

Nothing found by any of the checks.

Inconclusive, the rename crossed the page width: 15-chain-both, 26-op-modulo, 31-ctx-while.

## `hybrid`

Nothing found by any of the checks.

Inconclusive, the rename crossed the page width: 15-chain-both, 26-op-modulo, 31-ctx-while.

## `hybrid_stroustrup`

Nothing found by any of the checks.

Inconclusive, the rename crossed the page width: 15-chain-both, 26-op-modulo, 31-ctx-while.

