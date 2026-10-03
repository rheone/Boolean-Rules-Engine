# 16: If / ? :

**What to build:** If(condition, whenTrue, whenFalse) and the ? : form as a K3-aware conditional; an Unknown condition does not pick a branch. Every layer is updated: parser, compiler, evaluator, descriptors, printers, builder, JSON/YAML, schema and analyzer.

**Blocked by:** 09

**Status:** done

- [x] Semantics for an Unknown condition follow ADR-0005 and are verified against the oracle
- [x] DSL, JSON, YAML, builder, schema and printers support it
- [x] Analyzer understands If
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

`If(condition, whenTrue, whenFalse)` and the ternary `condition ? whenTrue : whenFalse` build one first-class `IfExpression(Condition, WhenTrue, WhenFalse)` (ADR-0005 decisions 3a, 8 and 13). **Semantics decision:** the reference specification (`.tmp/Strong Kleene K3 Logic.md` section 25) says an `Unknown` condition returns the branch value when both branches are the same definite value, otherwise `Unknown`. The naive multiplexer `(c AND t) OR (NOT c AND f)` disagrees (it gives `Unknown` for `If(Unknown, True, True)`), so the oracle's primitive definition is the multiplexer plus its consensus term, `(c AND t) OR (NOT c AND f) OR (t AND f)`, which is exactly the specification's rule (verified over all 27 rows by `IfTests`) and never changes a definite condition's result. I chose the consensus form over the bare multiplexer because the ticket's own wording ("an Unknown condition does not pick a branch") and the spec need `If(U, A, A) = A`; the same definition drives the analyzer rail and `K3Oracle.If`. **Evaluation:** a definite condition evaluates only its branch (the other is `NotEvaluated` in the tree and trace, with the predicate never invoked); an `Unknown` condition evaluates both; `Exhaustive` evaluates both. **Syntax:** `If` is a reserved word (any case) taking exactly three operands (`MalformedTree` otherwise). The lexer gained a `Question` token for a lone `?` (it was a lexical error; `??` is unchanged). A new `ParseExpression` entry point (root, parentheses, call arguments) wraps the OR-level parse and handles `? :`; the ternary is the lowest-precedence construct and, per ADR-0005 decision 8, its condition and each branch must be a single operand or a parenthesized group: a bare `AND`/`OR` chain, a bare infix expression or an unparenthesized nested ternary is `AmbiguousOperatorMixing`. The canonical printer writes `If(a, b, c)`; the label is `If` in every `OperatorStyle` (no symbolic/C-style spelling). Other layers: `RuleNode`, `RuleNodeCompiler`, `NodeShape`, `OperatorInfo`, `TreeFormatOpNames` (`if`), JSON/YAML parsers (printers need no change), schema enum, `RuleBuilder.If`, `Analyzer` (`If` rail composed from the And/Or/Not rails), README, CONTEXT.md, ADR-0005. Tests: `IfTests` (oracle over every assignment for all spellings, branch picking, Unknown-condition rule, skipping, exhaustive mode, mixing/nesting/syntax errors, JSON/YAML/builder/labels) plus the parity, shape, descriptor, schema, round-trip property and analyzer-vs-oracle generators.
