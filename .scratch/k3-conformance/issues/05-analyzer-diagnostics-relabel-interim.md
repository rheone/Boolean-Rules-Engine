# 05: Analyzer diagnostics relabelled (interim)

**What to build:** The existing classical-logic analyzer diagnostics are relabelled so they no longer claim K3 truths (A AND NOT A is Unknown when A is Unknown). This is a stopgap until the K3-aware analyzer lands.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] Diagnostic codes/messages state they are two-valued (classical) findings
- [ ] No message claims a K3 contradiction or tautology
- [ ] Existing analyzer tests updated
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
