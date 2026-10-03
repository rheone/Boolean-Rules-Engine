# 15: COALESCE / ??

**What to build:** COALESCE / ?? replaces only Unknown with the next operand; True and False pass through unchanged. Every layer is updated: parser, compiler, evaluator, descriptors, printers, builder, JSON/YAML, schema and analyzer.

**Blocked by:** 09

**Status:** ready-for-agent

- [ ] Binary, ternary and n-ary forms match the oracle
- [ ] ?? works in the DSL with precedence documented
- [ ] Analyzer understands COALESCE
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
