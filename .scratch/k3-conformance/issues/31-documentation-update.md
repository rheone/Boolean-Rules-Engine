# 31: Documentation update

**What to build:** README, CONTEXT.md and ADRs describe the new K3 language surface, notation, boundaries, transforms and diagnostics.

**Blocked by:** 19, 27, 29

**Status:** ready-for-agent

- [ ] README documents operators, notations, Project/Collapse, transforms and diagnostics
- [ ] CONTEXT.md glossary updated
- [ ] ADR cross-references are consistent
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
