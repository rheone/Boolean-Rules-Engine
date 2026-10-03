# 11: NAND and NOR

**What to build:** NAND / ↑ and NOR / ↓ as first-class nodes (negated conjunction/disjunction). Every layer is updated: parser, compiler, evaluator, descriptors, printers, builder, JSON/YAML, schema and analyzer.

**Blocked by:** 09

**Status:** ready-for-agent

- [ ] Truth tables match the oracle (binary)
- [ ] All notations, serialization, printers and schema support them
- [ ] Analyzer handles them
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
