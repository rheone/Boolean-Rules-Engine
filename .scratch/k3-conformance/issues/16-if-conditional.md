# 16: If / ? :

**What to build:** If(condition, whenTrue, whenFalse) and the ? : form as a K3-aware conditional; an Unknown condition does not pick a branch. Every layer is updated: parser, compiler, evaluator, descriptors, printers, builder, JSON/YAML, schema and analyzer.

**Blocked by:** 09

**Status:** ready-for-agent

- [ ] Semantics for an Unknown condition follow ADR-0005 and are verified against the oracle
- [ ] DSL, JSON, YAML, builder, schema and printers support it
- [ ] Analyzer understands If
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
