# 04: Lenient/failed substitution uses Unknown

**What to build:** A node that fails (lenient mode or failed-node substitution) becomes Unknown instead of False, so errors can never look like negative answers.

**Blocked by:** 03

**Status:** done

- [x] Lenient-mode failed node evaluates to Unknown
- [x] No remaining code path substitutes False for a failure
- [x] Existing lenient-mode tests updated to the new expectation
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

Lenient mode already compiled an unregistered predicate to a permanently Unknown term (ticket 03 era). The remaining False substitutions were the placeholders `RuleNodeCompiler` builds for nodes that fail validation (error node, depth/node-limit overflow, bad arity, bad threshold, unknown predicate in strict mode). They now all use the single `FailedNode.Placeholder` (an Unknown constant). These placeholders are only reachable alongside an error diagnostic (the tree is discarded), so the substitution is not observable through `Compile`; it is verified by `FailedNode` / `RuleNodeCompilerErrorNodeAndDefensiveThrowsTests` against the internal seam. Existing lenient-mode tests already expected Unknown; the error-node test that expected a False constant was updated deliberately.
