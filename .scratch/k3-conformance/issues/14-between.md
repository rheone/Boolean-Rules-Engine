# 14: BETWEEN

**What to build:** BETWEEN(min, max, ...) as AND(AtLeast(min, ...), AtMost(max, ...)). Every layer is updated: parser, compiler, evaluator, descriptors, printers, builder, JSON/YAML, schema and analyzer.

**Blocked by:** 13

**Status:** ready-for-agent

- [ ] Matches the oracle for all {T,F,U} inputs
- [ ] Invalid bounds (min > max, negative) give a readable diagnostic
- [ ] All notations, serialization, printers and schema support it
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
