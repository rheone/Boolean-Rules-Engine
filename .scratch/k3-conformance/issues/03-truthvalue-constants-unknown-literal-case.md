# 03: TruthValue constants, Unknown literal, case-insensitivity

**What to build:** Constants are TruthValue end to end so a rule can contain Unknown. True, False, Unknown and every operator name are case-insensitive; the canonical printer emits upper camel / upper case. Every layer is updated: parser, compiler, evaluator, descriptors, printers, builder, JSON/YAML, schema and analyzer.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] Unknown literal parses, builds, prints and round-trips in DSL, JSON and YAML; JSON schema updated
- [ ] Mixed-case constants and operator names compile to the same tree
- [ ] Canonical printer output is consistent upper camel / upper case
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
