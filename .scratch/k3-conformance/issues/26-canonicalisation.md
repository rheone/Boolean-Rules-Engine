# 26: Canonicalisation

**What to build:** Equivalent rules (under the K3-sound rewrite set) are given a single deterministic representation, so rules can be compared and de-duplicated.

**Blocked by:** 23

**Status:** done

- [x] Canonical form is deterministic and idempotent
- [x] Evaluation equals the original for all assignments
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

Implemented `CompiledRule.Canonicalize()` (internal `Canonicalizer`). Rule set and rationale are in README "Canonical form" and ADR-0005 decision 10. Decisions: sort key is the ordinal canonical text of each operand (so `NOT a` sorts before `a`; stable, not meaningful); aliases that would grow the tree (`NONE`, `NAND`, `NOR`, `IMPLIES`, `Project`, inspections) are deliberately not rewritten so the canonical form is never larger than its input, which ticket 27 relies on; `COALESCE` flattens but never reorders. The API remarks state that evaluation order, short-circuiting and fault reporting may change while the value does not. Tests: oracle-backed property test over generated rules (value, idempotence, determinism, size) and a property test that randomly permuted, regrouped, duplicated, aliased and double-negated spellings of an AND/OR/NOT rule canonicalise to the same text.
