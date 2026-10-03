# 25: Compress to non-primitive forms

**What to build:** An opt-in transform that rewrites expanded primitives back into readable derived operators where possible.

**Blocked by:** 23

**Status:** done

- [x] Compress(Expand(rule)) evaluates equal to the rule for all assignments
- [x] Result is no larger than the expanded form
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

Implemented `CompiledRule.CompressToDerived()` (internal `Compressor`, plus shared `ExpressionTools` for child mapping and tree size). Top-down pattern matching with a repeat-until-stable loop, so the result is idempotent and, because every pattern replaces a shape with an equal-or-smaller one, never larger than the input. Decisions: `COALESCE(x, False)` becomes `Project(x, False)` (not `IsTrue`); only the exact shapes `PrimitiveExpander` emits are matched for `XOR`/`EQUIVALENT`/`If`/`NXOR` (with the outer `OR` commutative); `NOT AtLeast(k)`/`NOT AtMost(k)` flip the comparison. Verified by an oracle-backed property test (random rules, all 27 assignments, original vs `Compress(Expand(rule))` vs oracle, node count, idempotence) and per-pattern tables. README "Compress to derived operators" and ADR-0005 decision 10 updated.
