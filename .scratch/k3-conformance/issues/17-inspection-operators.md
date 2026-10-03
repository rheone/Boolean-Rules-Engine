# 17: Inspection operators

**What to build:** IsTrue, IsFalse, IsUnknown and IsKnown test a result's state without collapsing the enclosing expression (they always yield a definite True or False). Every layer is updated: parser, compiler, evaluator, descriptors, printers, builder, JSON/YAML, schema and analyzer.

**Blocked by:** 03, 06

**Status:** ready-for-agent

- [ ] Each operator matches the oracle
- [ ] An inspection inside a larger rule does not fault or collapse the rest
- [ ] Analyzer understands inspection
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
