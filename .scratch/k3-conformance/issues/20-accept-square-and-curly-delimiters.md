# 20: Accept [] and {} grouping

**What to build:** (), [] and {} are interchangeable grouping delimiters in the DSL; the AST does not retain the written delimiter. Mismatched or unclosed delimiters are reported with a precise location.

**Blocked by:** 03

**Status:** done

- [x] Equivalent rules written with different delimiters compile to equal trees
- [x] Mismatched/unclosed delimiter diagnostics give the exact span
- [x] Round-trip property tests still pass
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

Lexer gained `{`/`}` tokens; every grouping site in `DslParser` accepts any of `()`, `[]`, `{}` via `IsGroupOpener`/`ExpectClose`, and the node keeps no delimiter, so equivalent rules compile to equal trees and equal canonical text. The ternary no-mixing check (`IsWrappedInParentheses`) now treats any delimiter kind as a group.

Decisions: function-call and term argument lists stay `(` only (ADR-0005 decision 9 says "grouping"; the reference material only shows sub-expression delimiters). `[` already existed as the array argument-value opener, which cannot collide because grouping only starts where an expression is expected. After a mismatched closer the parser consumes it as if it were the intended one, so deeply wrongly nested input can produce a follow-on diagnostic for the enclosing group; the first diagnostic is always the precise one. A leading closer (`] a`) is reported as a missing operand rather than "unexpected closing". One existing test (`DslParserTests` trailing garbage) used a stray `)` as its garbage and now uses `a AND b c`, because a stray closer has its own diagnostic.
