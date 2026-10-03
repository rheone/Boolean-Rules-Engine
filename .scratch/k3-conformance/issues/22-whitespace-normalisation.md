# 22: Whitespace normalisation and operator spacing

**What to build:** Printed rule text has whitespace collapsed to a single space, is trimmed, and has spaces around operators.

**Blocked by:** 09

**Status:** ready-for-agent

- [ ] Output is deterministic for any input spacing
- [ ] Re-parsing normalised text gives an equal tree
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
