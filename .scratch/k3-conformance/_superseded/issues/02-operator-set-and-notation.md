# 02: Operator set, aliases, notation

**Status:** ready-for-agent (decisions 7 and 8 in ADR-0005)
**Blocked by:** 01

**What to build:** Add `IMPLIES`, `EQUIVALENT` (aliases `IFF`, `XNOR`), `NAND`, `NOR`, `NXOR` (parity), and the `Unknown` literal as first-class nodes across AST, DSL, JSON, YAML, `RuleBuilder`, `OperatorInfo.Describe`, canonical printer, BDD analyzer, and schema (`rule-tree.schema.json`).

- [ ] Case-insensitive operators and `True`/`False`/`Unknown` literals; canonical form upper camel / upper
- [ ] Symbol aliases on input (`&&`, `||`, `!`, `∧ ∨ ¬ ⊕ → ↔ ↑ ↓`, `??`) map to canonical nodes; canonical printer stays word-only
- [ ] `XNOR` input and `xnor` JSON/YAML op still compile and re-emit as `EQUIVALENT`/`equivalent`
- [ ] `NXOR` parity truth table from `.tmp/xor.md`, including the `Unknown` rule; `ExactlyOne` unchanged
- [ ] `IMPLIES` = `¬A ∨ B` verified against oracle from 01
- [ ] Round-trip DSL/JSON/YAML for every new operator
- [ ] `XOR` with 3+ operands stays `XorArityViolation`, message hints at `NXOR`
- [ ] Any two different infix operators (other than `NOT`>`AND`>`OR`) at one level without parentheses is a compile error
- [ ] Update README and CONTEXT.md operator tables
