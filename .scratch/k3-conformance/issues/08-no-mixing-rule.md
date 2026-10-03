# 08: No-mixing operator rule

**What to build:** Mixing different operators in one group requires explicit parentheses, except the NOT > AND > OR precedence, so no one is surprised by implicit precedence.

**Blocked by:** 03

**Status:** ready-for-agent

- [ ] Ambiguous mixing is a diagnostic with a precise location and a suggestion to add parentheses
- [ ] NOT > AND > OR continues to parse without parentheses
- [ ] Existing valid rules and round-trips are unaffected
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
