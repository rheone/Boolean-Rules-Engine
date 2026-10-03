# 06: Rename NXOR to PARITY

**What to build:** The n-ary parity operator is renamed PARITY because NXOR conventionally means negated XOR (XNOR), the opposite of what it does, and XNOR is already an alias of EQUIVALENT (spec audit section D). Remove the NXOR spelling entirely in DSL, JSON/YAML (op name), schema, builder, node and shape names, labels, printers, rewrites and analyzer; an attempt to use NXOR yields a did-you-mean hint pointing at PARITY. Semantics unchanged (Unknown if any operand is Unknown, otherwise True for an odd number of True). Update the XOR arity hint message to name PARITY. Owner decision recorded 2026-10-03.

**Blocked by:** 05

**Status:** ready-for-agent

- [ ] PARITY compiles and evaluates identically to the former NXOR (oracle-checked up to 4 operands)
- [ ] NXOR is rejected with a did-you-mean suggestion for PARITY, in DSL, JSON and YAML
- [ ] XOR with 3 or more operands points at PARITY
- [ ] All layers, tests, README, CONTEXT.md and ADR-0005 decision 4 updated
- [ ] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

Source: [spec audit](../../k3-conformance/spec-audit.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
