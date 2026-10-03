# 13: ANY, ALL, NONE

**What to build:** ANY, ALL and NONE cardinality aliases using the definitely-true/possibly-true interval so Unknown operands give correct bounds. Every layer is updated: parser, compiler, evaluator, descriptors, printers, builder, JSON/YAML, schema and analyzer.

**Blocked by:** 03, 06

**Status:** ready-for-agent

- [ ] Truth tables match the oracle up to 4 operands including Unknown inputs
- [ ] All notations, serialization, printers and schema support them
- [ ] Analyzer handles them
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
