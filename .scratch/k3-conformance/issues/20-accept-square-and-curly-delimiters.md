# 20: Accept [] and {} grouping

**What to build:** (), [] and {} are interchangeable grouping delimiters in the DSL; the AST does not retain the written delimiter. Mismatched or unclosed delimiters are reported with a precise location.

**Blocked by:** 03

**Status:** ready-for-agent

- [ ] Equivalent rules written with different delimiters compile to equal trees
- [ ] Mismatched/unclosed delimiter diagnostics give the exact span
- [ ] Round-trip property tests still pass
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
