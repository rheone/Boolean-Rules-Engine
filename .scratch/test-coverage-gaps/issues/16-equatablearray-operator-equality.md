# 16: Cover EquatableArray's `==`/`!=` operators

**What to build:** `EquatableArray<T>.operator ==` and `operator !=` (src/TruthWeaver.Abstractions/EquatableArray.cs, lines ~30-42) have no test coverage per the ticket 11 Cobertura report. This struct is the structural-equality primitive underneath `TermIdentity.Arguments` and AST node argument lists — the ADR-0003 canonical round-trip guarantee and per-evaluation memoization both depend on it comparing correctly, but only the instance `Equals(EquatableArray<T>)` method is currently exercised, never the operators that delegate to it.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `left == right` returns `true` for two `EquatableArray<T>` instances with equal elements in the same order, and `false` for a different order or different elements.
- [x] `left != right` is the logical negation of `==` for the same cases.
- [x] Both operators are exercised against the `Empty` array on at least one side.
- [x] Existing `EquatableArray`/`TermIdentity` tests continue to pass unchanged.
