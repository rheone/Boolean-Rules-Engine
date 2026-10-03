# 06: K3-aware (dual-rail) analyzer

**What to build:** The analyzer reasons in K3 using a dual-rail BDD (definitely true / possibly true), so contradictions and tautologies are reported only when they hold for every {True, False, Unknown} assignment.

**Blocked by:** 03, 05

**Status:** ready-for-agent

- [ ] A AND NOT A and A OR NOT A are not reported as contradiction/tautology; genuine K3 ones are
- [ ] Analyzer results agree with the oracle over all assignments
- [ ] Max-terms cap behaviour preserved
- [ ] Interim relabel from 05 is removed or superseded
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
