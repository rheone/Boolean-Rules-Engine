# 11: NAND and NOR

**What to build:** NAND / ↑ and NOR / ↓ as first-class nodes (negated conjunction/disjunction). Every layer is updated: parser, compiler, evaluator, descriptors, printers, builder, JSON/YAML, schema and analyzer.

**Blocked by:** 09

**Status:** done

- [x] Truth tables match the oracle (binary)
- [x] All notations, serialization, printers and schema support them
- [x] Analyzer handles them
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

`NAND` / `↑` and `NOR` / `↓` are first-class, strictly binary `NandExpression` / `NorExpression` nodes (the spec's operator table says binary; a chain is rejected with the shared infix arity code `XorArityViolation` and a parentheses hint, like IMPLIES and EQUIVALENT). They were added to every layer following the IMPLIES template: lexer (`↑`, `↓` operator tokens), `DslParser` (reserved words, symbol aliases, infix-operator list so the no-mixing rule applies), `RuleNode`/`RuleNodeCompiler` (one shared `BuildNegatedBinary` helper), `Evaluator` (negated `KleeneAnd`/`KleeneOr`; both operands always evaluated, no short-circuit), `Analyzer` (`Not(And(..))` / `Not(Or(..))` on the dual rail, plus term collection), `ExpressionShape`/`OperatorInfo`, `TreeFormatOpNames` (`nand`, `nor`), JSON/YAML parsers, `rule-tree.schema.json`, `CanonicalPrinter` (always parenthesised), `RuleRenderTree` (symbolic `↑`/`↓`; the C-style keeps the words), and `RuleBuilder.Nand`/`Nor`. `K3Oracle.Nand`/`Nor` are built from `Not` and `And`/`Or` only. `NandNorTests` plus the conformance, analyzer-vs-oracle and DSL round-trip generators cover every spelling over all {T,F,U} assignments. No dedicated expand-to-NAND-only transform here; that stays with ticket 24.
