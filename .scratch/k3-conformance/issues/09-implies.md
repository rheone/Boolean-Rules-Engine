# 09: IMPLIES

**What to build:** IMPLIES / → as Strong Kleene material implication (NOT A OR B), a first-class tree node with its own evaluation, description and printing; its primitive definition is the oracle's definition. Every layer is updated: parser, compiler, evaluator, descriptors, printers, builder, JSON/YAML, schema and analyzer.

**Blocked by:** 06, 08

**Status:** ready-for-agent

- [ ] Truth table matches the oracle for all {T,F,U} inputs
- [ ] DSL, symbol, JSON, YAML, builder, schema, printers and Mermaid support it
- [ ] Analyzer handles it in the dual-rail BDD
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
