# 01: Unblock CI

**What to build:** A CI build (CI=true promotes warnings to errors) succeeds again. Resolve the two TODO comments in the operator-info code (S1135) and the following blank-line style finding (SA1512), and the S6966 finding in the benchmarks entry point, by doing the work the TODOs describe or deleting the stale ones (the 'add all operators' TODO is stale: every operator is covered). Findings: research items 6c.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Building with CI=true produces no warnings-as-errors
- [x] `dotnet format --verify-no-changes --severity info` exits 0
- [x] No analyzer was suppressed or downgraded to achieve this
- [x] Tests still pass
- [x] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

Source: [research findings](../../k3-conformance/research-findings.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

- Removed the stale "add all operators" TODO (every operator is covered) and turned the other into a plain note, which also clears the SA1512 finding. The benchmarks entry point now awaits `RunAsync` (S6966). A `CI=true` build has 0 warnings and `dotnet format --verify-no-changes --severity info` exits 0. No analyzer was suppressed.
