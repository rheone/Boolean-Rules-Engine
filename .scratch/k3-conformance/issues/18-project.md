# 18: Project

**What to build:** Project(expr, True|False) as an in-tree node that replaces Unknown with a chosen definite value. Every layer is updated: parser, compiler, evaluator, descriptors, printers, builder, JSON/YAML, schema and analyzer.

**Blocked by:** 15

**Status:** ready-for-agent

- [ ] Project yields a definite value for every input and matches the oracle
- [ ] Known values pass through unchanged
- [ ] Analyzer understands Project
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
