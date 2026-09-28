# 32: Cover CompiledRule.ToString()

**What to build:** `CompiledRule<TContext>.ToString()` (src/TruthWeaver/Evaluation/CompiledRule.cs, line ~139, `return this.CanonicalText;`) has no test coverage, merged across all six Cobertura reports from ticket 11's coverage run. Lowest priority of this pass's findings — it's a trivial one-line delegation to an already well-tested property — but it's still zero-coverage public API surface.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Calling `ToString()` on a compiled rule returns the same text as its `CanonicalText` property.
- [x] Existing `CompiledRule` tests continue to pass unchanged.
