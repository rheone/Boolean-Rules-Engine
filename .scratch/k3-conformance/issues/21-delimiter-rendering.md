# 21: Delimiter rendering

**What to build:** The printer normalises every delimiter to parentheses; an optional printer varies delimiters by nesting depth for readability.

**Blocked by:** 20

**Status:** ready-for-agent

- [ ] Default output uses parentheses only
- [ ] Depth-varying printer is opt-in and re-parses to an equal tree
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
