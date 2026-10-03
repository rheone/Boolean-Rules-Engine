# 03: Roslynator on the whole solution

**What to build:** The solution-wide Roslynator analysis works on the .slnx and covers every project locally, in the Husky task runner and in CI (today the loop covers only 4 of 12 projects). Upgrade the Roslynator CLI tool to a version with .slnx support (research item 6b: 1.0.0 verified locally; the CI runner is untested).

**Blocked by:** 01, 02

**Status:** ready-for-agent

- [ ] `dotnet roslynator analyze` works against the solution or a documented loop covers all projects
- [ ] Tool version updated in the local tool manifest with the lock/restore flow still working
- [ ] Husky and CI steps updated to match
- [ ] Issues-log row 2 closed
- [ ] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

Source: [research findings](../../k3-conformance/research-findings.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
