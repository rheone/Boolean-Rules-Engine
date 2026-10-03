# 24: Expand to NAND-only / NOR-only

**What to build:** Opt-in transforms that rewrite a rule into NAND-only or NOR-only form (e.g. NOT A = A NAND A).

**Blocked by:** 23

**Status:** done

- [x] Evaluation is identical to the original for all assignments
- [x] Output contains only the target gate
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

Added public `CompiledRule<TContext>.ExpandToNand()` and `ExpandToNor()` (engine: `TruthWeaver.Rewriting.UniversalGateExpander`, built on `PrimitiveExpander`). Each returns a new rule, leaves the original untouched and keeps the `CollapsePolicy`. Property tests (generated rules, every {T,F,U} assignment, original vs oracle vs rewritten) and operator-by-operator tests pass for both gates; textbook identities are pinned by exact canonical text.

Decisions (ADR-0005 decision 10): thresholds are rewritten as a disjunction over k-subsets of conjunctions (monotone, so exact for `Unknown`), `AtMost(k) = NOT AtLeast(k+1)`, `Exactly` drops a vacuous side; `COALESCE` (and `Project`/inspections) cannot be expressed with NAND/NOR because gate circuits are monotone in the information order and `COALESCE` is not, so it stays as a documented boundary with its operands rewritten, and the property test allows only the gate plus `coalesce` for rules that use it. The generated-rule property tests skip rules whose text exceeds 30 characters because wide thresholds over nested operands grow combinatorially.
