# 13: ANY, ALL, NONE

**What to build:** ANY, ALL and NONE cardinality aliases using the definitely-true/possibly-true interval so Unknown operands give correct bounds. Every layer is updated: parser, compiler, evaluator, descriptors, printers, builder, JSON/YAML, schema and analyzer.

**Blocked by:** 03, 06

**Status:** done

- [x] Truth tables match the oracle up to 4 operands including Unknown inputs
- [x] All notations, serialization, printers and schema support them
- [x] Analyzer handles them
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

`ANY`, `ALL` and `NONE` are first-class n-ary function-call nodes (`AnyExpression`, `AllExpression`, `NoneExpression`; ADR-0005 decisions 3a and 6) with the cardinality-interval semantics of `AtLeast(1, ...)`, `AtLeast(n, ...)` and `AtMost(0, ...)`. Decision: they need **two or more** operands, consistent with `AND`/`OR`/`ExactlyOne`/`NXOR` (fewer is `MalformedTree`); the threshold family's one-operand allowance is not inherited since a one-operand `ANY`/`ALL` is just the operand and `NONE` its negation. Added to every layer following the ExactlyOne/NXOR templates: `DslParser` (reserved words; the NXOR parse path was generalised into one `ParseOperandCall` helper), `RuleNode`/`RuleNodeCompiler`, `Evaluator` (delegates to the threshold evaluator; own `ANY`/`ALL`/`NONE` labels; all operands evaluated), `Analyzer` (existing `AtLeast` rail; `NONE` as its negation), `ExpressionShape`/`OperatorInfo`, `TreeFormatOpNames` (`any`, `all`, `none`), JSON/YAML parsers and schema, `CanonicalPrinter`, and `RuleBuilder.Any`/`All`/`None`. Printers keep the word in every `OperatorStyle`. `K3Oracle.Any`/`All`/`None` use the `Cardinality` interval helper only. `AnyAllNoneTests` checks 2..4 operands over every {T,F,U} assignment (also against the equivalent threshold rule), plus spellings, reserved words, operand minimum, JSON/YAML round trips, builder, labels, descriptions and printers; the shared parity/exhaustiveness tests, analyzer-vs-oracle generator and DSL round-trip generator include the three operators. Note: in K3 the three tables coincide with `OR`, `AND` and `NOT OR`; they remain distinct nodes so the author's operator round-trips (documented in CONTEXT.md).
