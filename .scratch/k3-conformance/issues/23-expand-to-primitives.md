# 23: Expand to primitives

**What to build:** An opt-in transform on the immutable tree that expands derived operators into the primitive kernel (NOT, AND, OR, AtLeast, AtMost, Exactly, COALESCE).

**Blocked by:** 10, 11, 12, 13, 14, 17

**Status:** done

- [x] For any rule and any {T,F,U} assignment, the expanded rule evaluates equal to the original (property test)
- [x] Only primitive nodes remain in the output
- [x] The original tree is untouched
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

Added the public `CompiledRule<TContext>.ExpandToPrimitives()` (internal engine: `TruthWeaver.Rewriting.PrimitiveExpander`). It returns a new rule, leaves the original untouched and keeps the declared `CollapsePolicy`; every derived operator, the four inspections and `Project` expand into `NOT`/`AND`/`OR`/`AtLeast`/`AtMost`/`Exactly`/`COALESCE`, with no semantic boundary left. The generator from `AnalyzerTests` moved to `TestSupport/K3RuleGenerator` so the analyzer and rewrite property tests share it; `K3Rule` gained `Compiled` and `Rewrite`.

Decisions (recorded in ADR-0005 decision 10): `NXOR` expands to `OR(Exactly(1, ...), Exactly(3, ...), ...)` over the odd counts (K3-correct and linear, unlike a fold of the XOR expansion which is exponential); the inspections are expressed with `COALESCE` (`IsUnknown(x) = COALESCE(x, True) AND COALESCE(NOT x, True)`), so none stays as a boundary; `BETWEEN` drops a vacuous bound because `AtLeast(0)`/`AtMost(n)` are compiler-rejected constants; `GreaterThan(k)`/`LessThan(k)` shift `k` by one. Expansion repeats operands the definitions mention twice, so printed text can grow and exceed the default compile node limit (documented; evaluation is unaffected).
