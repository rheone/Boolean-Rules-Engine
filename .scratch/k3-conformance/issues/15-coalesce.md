# 15: COALESCE / ??

**What to build:** COALESCE / ?? replaces only Unknown with the next operand; True and False pass through unchanged. Every layer is updated: parser, compiler, evaluator, descriptors, printers, builder, JSON/YAML, schema and analyzer.

**Blocked by:** 09

**Status:** done

- [x] Binary, ternary and n-ary forms match the oracle
- [x] ?? works in the DSL with precedence documented
- [x] Analyzer understands COALESCE
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

`COALESCE(a, b, ...)` and the infix `a ?? b` build one first-class `CoalesceExpression(Operands)` (ADR-0005 decisions 3a and 8): the first operand that is not `Unknown`, with `True`/`False` passing through. Decisions: two or more operands (`MalformedTree` otherwise); `??` is infix under the no-mixing rule (mixing with `AND`/`OR` or another infix operator without parentheses is `AmbiguousOperatorMixing`, naming `??` in the message); **chains `a ?? b ?? c` are accepted** and fold into one n-ary node because the operator is associative (the binary-only infix operators keep rejecting chains); only the `??` token is infix, the word `COALESCE` is a function call only (the lexer gained a doubled `??` token; a lone `?` stays a lexical error). The canonical printer writes `COALESCE(a, b, c)`. Evaluation reuses the AND/OR chain helper (now taking a stop predicate): operands run left to right, the rest are skipped and marked `NotEvaluated` once a known value appears, `Exhaustive` mode evaluates all. Analyzer: right fold of `(D_x OR (P_x AND D_y), P_x AND (D_x OR P_y))` (derivation in the `Coalesce` XML doc), verified by the analyzer-vs-oracle generator. Tree printers spell the label `COALESCE` / `??` / `??` (word / symbolic / C-style). Other layers: `RuleNode`, `RuleNodeCompiler`, `NodeShape`, `OperatorInfo`, `TreeFormatOpNames` (`coalesce`), JSON/YAML parsers (printers need no change), schema enum, `RuleBuilder.Coalesce`. `K3Oracle.Coalesce` picks the first non-Unknown operand (no K3 connective can detect Unknown). `CoalesceTests` checks 2..4 operands over every {T,F,U} assignment for both spellings plus precedence, chains, mixing, skipping, JSON/YAML, builder, labels and printers. README, CONTEXT.md and ADR-0005 updated.
