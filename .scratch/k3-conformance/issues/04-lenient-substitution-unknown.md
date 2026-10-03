# 04: Lenient/failed substitution uses Unknown

**What to build:** A node that fails (lenient mode or failed-node substitution) becomes Unknown instead of False, so errors can never look like negative answers.

**Blocked by:** 03

**Status:** ready-for-agent

- [ ] Lenient-mode failed node evaluates to Unknown
- [ ] No remaining code path substitutes False for a failure
- [ ] Existing lenient-mode tests updated to the new expectation
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
