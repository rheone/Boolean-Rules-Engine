# 12: NXOR and the XOR arity hint

**What to build:** n-ary parity NXOR, which is Unknown whenever any operand is Unknown; binary XOR with 3 or more operands is an error that points at NXOR. ExactlyOne keeps meaning exactly-one-true. Every layer is updated: parser, compiler, evaluator, descriptors, printers, builder, JSON/YAML, schema and analyzer.

**Blocked by:** 09

**Status:** ready-for-agent

- [ ] NXOR matches the oracle for up to 4 operands
- [ ] XOR with 3+ operands yields a diagnostic naming NXOR
- [ ] ExactlyOne behaviour is unchanged and tested to differ from NXOR
- [ ] Analyzer handles NXOR
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
