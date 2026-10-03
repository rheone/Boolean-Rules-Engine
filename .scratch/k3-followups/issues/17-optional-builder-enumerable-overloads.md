# 17: Optional: RuleBuilder IEnumerable overloads

**What to build:** RuleBuilder overloads taking IEnumerable<RuleBuilder> for dynamic operand lists, returning a constant or the single operand for 0 or 1 items where the operator would otherwise reject them. Low priority. Research item 4.

**Blocked by:** 06

**Status:** ready-for-agent

- [ ] Overloads exist for the n-ary operators with documented 0 and 1 item behaviour
- [ ] Tests cover empty, single and many items
- [ ] README builder table updated
- [ ] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

Source: [research findings](../../k3-conformance/research-findings.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
