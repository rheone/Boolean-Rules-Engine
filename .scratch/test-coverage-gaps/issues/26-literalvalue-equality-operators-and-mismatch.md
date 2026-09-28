# 26: Cover LiteralValue's `==`/`!=` operators and cross-kind Equals mismatch

**What to build:** `LiteralValue.operator ==`/`!=` (src/TruthWeaver.Abstractions/LiteralValue.cs, lines ~57-69) and the `this.Kind != other.Kind` early-`false` branch of `Equals(LiteralValue)` (line ~241) have no test coverage across any test project's Cobertura report (merged across all six reports produced by ticket 11's coverage run). Lower priority than the `TermIdentity`/`EquatableArray` gaps ([[18-termidentity-equals-null-and-reference-fastpath]], [[16-equatablearray-operator-equality]]) since `LiteralValue.Equals(LiteralValue)`'s same-kind branches are already well exercised — only the operators and the cross-kind short-circuit are missing.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `left == right` and `left != right` are exercised for two equal `LiteralValue`s and two unequal ones of the same kind.
- [x] Comparing two `LiteralValue`s of different `LiteralKind` (e.g. an `Int64` against a `String`) via `Equals` returns `false` without inspecting either value's payload.
- [x] Existing `LiteralValue` equality tests continue to pass unchanged.
