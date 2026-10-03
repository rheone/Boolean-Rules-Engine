# 05: Grouping delimiters

**Status:** ready-for-agent after 02
**Blocked by:** 02

**What to build:** `()`, `[]`, `{}` interchangeable grouping per `.tmp/Grouping Delimiters and Rendering.md`; delimiter choice never affects evaluation or AST equality.

- [ ] Lexer/parser accept all three pairs; mismatched/unclosed/misnested produce diagnostics with spans
- [ ] Printer option: normalize all groups to parentheses (default for canonical output)
- [ ] Printer option: deterministic delimiter cycling by nesting depth for readability
- [ ] AST does not retain the written delimiter (ADR-0005 #9); `[a AND b]` and `(a AND b)` compile to equal trees
- [ ] Interaction with `dsl-escaping` ticket checked (`[` `{` inside string literals)
