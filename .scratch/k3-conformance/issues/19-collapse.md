# 19: Collapse

**What to build:** Collapse(expr, policy) as the evaluation-API boundary (and an outermost-only DSL function) that turns a K3 result into a two-valued answer. UnknownIsError produces an explicit rejected-unresolved outcome with no Fault. Decision.IsSatisfied stays fail-closed.

**Blocked by:** 02, 18

**Status:** ready-for-agent

- [ ] Each policy is exercised against the oracle
- [ ] UnknownIsError distinguishes not-known from something-broke and records no Fault
- [ ] Collapse nested inside a rule is rejected with a diagnostic
- [ ] Decision.IsSatisfied is still true only for True
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
