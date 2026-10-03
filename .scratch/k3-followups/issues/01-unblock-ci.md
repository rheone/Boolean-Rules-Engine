# 01: Unblock CI

**What to build:** A CI build (CI=true promotes warnings to errors) succeeds again. Resolve the two TODO comments in the operator-info code (S1135) and the following blank-line style finding (SA1512), and the S6966 finding in the benchmarks entry point, by doing the work the TODOs describe or deleting the stale ones (the 'add all operators' TODO is stale: every operator is covered). Findings: research items 6c.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Building with CI=true produces no warnings-as-errors
- [ ] `dotnet format --verify-no-changes --severity info` exits 0
- [ ] No analyzer was suppressed or downgraded to achieve this
- [ ] Tests still pass
- [ ] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

Source: [research findings](../../k3-conformance/research-findings.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
