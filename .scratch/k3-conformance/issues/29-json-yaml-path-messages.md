# 29: JSON/YAML path-based messages

**What to build:** The same readable diagnostics for malformed JSON and YAML rules, located by JSON/YAML path instead of line and column.

**Blocked by:** 28

**Status:** ready-for-agent

- [ ] Malformed JSON and YAML trees give code, explanation, path, expected and found
- [ ] Plain-text rendering matches the DSL style
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
