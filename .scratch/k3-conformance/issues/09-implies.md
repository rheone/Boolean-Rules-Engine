# 09: IMPLIES

**What to build:** IMPLIES / → as Strong Kleene material implication (NOT A OR B), a first-class tree node with its own evaluation, description and printing; its primitive definition is the oracle's definition. Every layer is updated: parser, compiler, evaluator, descriptors, printers, builder, JSON/YAML, schema and analyzer.

**Blocked by:** 06, 08

**Status:** done

- [x] Truth table matches the oracle for all {T,F,U} inputs
- [x] DSL, symbol, JSON, YAML, builder, schema, printers and Mermaid support it
- [x] Analyzer handles it in the dual-rail BDD
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

`IMPLIES` / `→` is a first-class `ImpliesExpression(Antecedent, Consequent)` node end to end. Evaluation is `Or(Not(a), b)` (both operands are always evaluated, like `XOR`; no short-circuit), the analyzer rail is the same primitive definition, and `K3Oracle.Implies` is built from `Not`/`Or` only; the exhaustive conformance test covers the word, lower-case and `→` spellings, and the random analyzer-vs-oracle generator and the DSL round-trip property generator both now produce `IMPLIES`. Touched layers: `DslParser` (`IMPLIES` keyword reserved, `→` lexed as an operator token, added to the infix-operator list from ticket 08 so no-mixing applies), `RuleNode`/`RuleNodeCompiler`, `Evaluator`, `Analyzer`, `ExpressionShape`/`OperatorInfo`, `TreeFormatOpNames` (`implies`), `CanonicalPrinter` (always parenthesised, `(a IMPLIES b)`), `RuleRenderTree` (symbolic `→`), `RuleBuilder.Implies`, JSON and YAML parsers, `rule-tree.schema.json`; `RuleDiff` needed no change (it compares node types and goes through the shape seam).

Decisions (also noted in ADR-0005 decision 13 and the issues log): operand-count errors (including a DSL chain `a IMPLIES b IMPLIES c`, which would otherwise need an arbitrary associativity) reuse `DiagnosticCodes.XorArityViolation` (BRE0006; its doc now says "XOR, XNOR or IMPLIES ... other than exactly two operands") with a "add parentheses" hint, rather than adding a new public code; the C-style tree-printer style has no implication spelling so `IMPLIES` keeps its word form there (ExactlyOne/threshold already behave that way); the canonical printer renames its internal `XorOperand` context to `InfixOperand`.
