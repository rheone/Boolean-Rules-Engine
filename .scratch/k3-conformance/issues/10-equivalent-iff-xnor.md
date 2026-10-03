# 10: EQUIVALENT / IFF / XNOR migration

**What to build:** EQUIVALENT / ↔ as the biconditional, with IFF as an alias and xnor kept as a legacy input so stored rules keep compiling. Every layer is updated: parser, compiler, evaluator, descriptors, printers, builder, JSON/YAML, schema and analyzer.

**Blocked by:** 09

**Status:** ready-for-agent

- [ ] Truth table matches the oracle
- [ ] Existing xnor text, JSON and YAML still compile with the same behaviour
- [ ] Canonical printer emits EQUIVALENT
- [ ] Analyzer handles it
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
