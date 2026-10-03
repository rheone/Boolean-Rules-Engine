# 14: BETWEEN

**What to build:** BETWEEN(min, max, ...) as AND(AtLeast(min, ...), AtMost(max, ...)). Every layer is updated: parser, compiler, evaluator, descriptors, printers, builder, JSON/YAML, schema and analyzer.

**Blocked by:** 13

**Status:** done

- [x] Matches the oracle for all {T,F,U} inputs
- [x] Invalid bounds (min > max, negative) give a readable diagnostic
- [x] All notations, serialization, printers and schema support it
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

`BETWEEN(min, max, op1, op2, ...)` is a first-class n-ary function-call node (`BetweenExpression(Min, Max, Operands)`; ADR-0005 decisions 3a and 6) defined as `AND(AtLeast(min, ...), AtMost(max, ...))` over the definitely-true / possibly-true interval. The two integer bounds are parsed like the threshold family's `k` (DSL `NumberLiteral`, JSON/YAML numeric `min`/`max`, builder `RuleBuilder.Between(min, max, ...)`). Decisions: **two or more operands** (as `ANY`/`ALL`); bounds must satisfy `0 <= min <= max <= n` and the whole range `0..n` is rejected as an always-true constant, all reported as `InvalidThresholdValue` with a message naming `min=`/`max=` and the allowed range; a missing or non-integer bound is a `SyntaxError`, a JSON/YAML node without numeric bounds is `MalformedTree`. Layers touched: `Lexer` untouched, `DslParser` (`ParseBetween`, reserved word), `RuleNode`/`RuleNodeCompiler` (`BuildBetween`), `Evaluator` (ANDs the two threshold evaluations; label `BETWEEN(min, max)`), `Analyzer` (`AtLeast(min)` AND NOT `AtLeast(max + 1)`), `NodeShape` (new optional `Max`, `K` carries `min`), `OperatorInfo`, `TreeFormatOpNames` (`between`), JSON/YAML parsers and printers (`min`/`max` keys), `rule-tree.schema.json` (`betweenOperatorNode`), `CanonicalPrinter`, `RuleDiff` (bounds are part of node identity) and `RuleBuilder`. Printers keep the word in every `OperatorStyle`. `K3Oracle.Between` is `And` of two `Cardinality` calls. `BetweenTests` checks 2..4 operands over every valid bound pair and every {T,F,U} assignment, plus spellings, diagnostics, JSON/YAML, builder, labels and printers; the shared parity/exhaustiveness tests, analyzer-vs-oracle generator and DSL round-trip generator include the operator. README, CONTEXT.md and ADR-0005 updated.
