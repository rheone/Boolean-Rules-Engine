# 28: Structured diagnostics and DSL messages

**What to build:** Malformed DSL produces structured diagnostics (code, plain explanation, span, expected versus found, optional did-you-mean suggestion) and a plain-text rendering, built on the existing diagnostic model.

**Blocked by:** 20

**Status:** ready-for-agent

- [ ] Unknown operators and aliases get did-you-mean suggestions
- [ ] Each message carries code, location, expected and found
- [ ] Plain-text rendering is available alongside the structured data
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
