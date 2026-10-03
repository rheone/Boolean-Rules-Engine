# 12: Ordinal-only EqualsConfigurable

**What to build:** EqualsConfigurable (and any related option) compares ordinally; culture-sensitive comparison is removed or restricted to an explicit, documented invariant-culture path, following the Microsoft string-comparison guidance. Findings: research item 7.2.

**Blocked by:** 10

**Status:** ready-for-agent

- [ ] No culture-sensitive comparison remains in the built-in predicates
- [ ] Trim and ignoreCase options keep working and are tested
- [ ] XML docs and README updated
- [ ] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

Source: [research findings](../../k3-conformance/research-findings.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
