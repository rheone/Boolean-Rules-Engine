# 10: EQUIVALENT / IFF / XNOR migration

**What to build:** EQUIVALENT / ↔ as the biconditional, with IFF as an alias and xnor kept as a legacy input so stored rules keep compiling. Every layer is updated: parser, compiler, evaluator, descriptors, printers, builder, JSON/YAML, schema and analyzer.

**Blocked by:** 09

**Status:** done

- [x] Truth table matches the oracle
- [x] Existing xnor text, JSON and YAML still compile with the same behaviour
- [x] Canonical printer emits EQUIVALENT
- [x] Analyzer handles it
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

`EQUIVALENT` / `IFF` / `XNOR` / `↔` is one first-class node, now `EquivalentExpression` (renamed from `XnorExpression`; a public record cannot be type-aliased, so this is a documented pre-1.0 break, see ADR-0005 decision 13). The DSL parser has a small word-alias table (`IFF`, `XNOR` -> `EQUIVALENT`) next to the symbol-alias table, so the no-mixing rule, chain handling and reserved words all work in every spelling; the lexer accepts `↔`. `TreeFormatOpNames` gained a read-only table (`xnor`, `iff` -> `Equivalent`) so JSON and YAML accept the legacy and alias ops while the printers write `equivalent`; the schema enum lists all three. `RuleBuilder.Equivalent` is new and `RuleBuilder.Xnor` forwards to it. Labels/descriptions, canonical text and the Word/Symbolic/CStyle printer styles use `EQUIVALENT` (`↔`, `==`). The arity error reuses `XorArityViolation` (BRE0006) with a message naming EQUIVALENT and hinting at parentheses, as for IMPLIES. `K3Oracle.Equivalent` already existed; `EquivalentTests` checks all spellings against it over every assignment, plus JSON/YAML legacy reads, builder, labels and printers. Existing tests that expected `XNOR` canonical text or labels were updated to `EQUIVALENT`.
