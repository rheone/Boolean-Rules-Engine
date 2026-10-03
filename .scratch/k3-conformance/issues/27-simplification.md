# 27: Simplification

**What to build:** An opt-in transform that replaces an expression with an equivalent, cheaper one using only K3-sound rewrites; classical-only rewrites such as A OR NOT A => True are never applied.

**Blocked by:** 06, 26

**Status:** done

- [x] Evaluation equals the original for all assignments (property test)
- [x] Known classical-only rewrites are demonstrably not applied
- [x] Output is never larger than the input
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

Implemented `CompiledRule.Simplify()` (internal `Simplifier`, built on `Canonicalizer`). The rule list, the classical laws deliberately not applied and the evaluation-order caveat are in README "Simplify" and ADR-0005 decision 10. Decisions: every candidate rewrite is either structurally smaller or size-guarded (derived operators with a constant operand go through `PrimitiveExpander.ExpandTop`, a new single-level entry point split out of `Expand`, and are kept only if no larger); `If` with an `Unknown` condition is left alone because the K3 definition keeps the consensus term; the analyzer's dual-rail knowledge is not used (it reports diagnostics, and the local structural rules are easier to verify); a final guard returns the input if the result were ever larger. Tests: oracle-backed property tests over generated rules (and over their expansions) for value, size and idempotence, a table of rewrites with expected text, and explicit tests that excluded middle, non-contradiction, `a IMPLIES a`, `a EQUIVALENT a`, `a XOR a` and complement absorption are not applied.
