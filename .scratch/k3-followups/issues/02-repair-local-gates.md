# 02: Repair local gates

**What to build:** `dotnet csharpier check .` runs to completion locally and in CI. Repair the dangling directory junction under the agent-skills folder that points at the old Boolean-Rules-Engine path (the crash cause), and add the missing final newline to the central package props file. A .csharpierignore alone does not help because CSharpier's version-check pass ignores ignore files (research item 6a).

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] `dotnet csharpier check .` exits 0 from the repo root
- [ ] The junction no longer points at a missing path (or is removed if it is only a local artifact; say which)
- [ ] Issues-log rows 1 and 3 are updated with the resolution
- [ ] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

Source: [research findings](../../k3-conformance/research-findings.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
