# 06: Derived logical gates: XOR, EQUIVALENT, IMPLIES, NAND, NOR, PARITY

**What to build:** One document each for the derived logical operations, with the canonical primitive form verified under Strong K3: XOR (binary), EQUIVALENT (aliases IFF and XNOR), IMPLIES (Kleene strong implication NOT A OR B, contrasted with Lukasiewicz), NAND, NOR and PARITY (n-ary parity, Unknown if any operand is Unknown; the former NXOR name is removed and not documented as an alias, with a note about why the name changed). Each states the arity, canonical form, truth table, whether a classical identity still holds, and edge cases such as XOR with more than two operands pointing at PARITY. A decomposition diagram only where it clarifies (for example the XOR and EQUIVALENT composition).

**Blocked by:** 05, k3-followups 06

**Status:** ready-for-agent

- [ ] Six documents conform to the template, each with a verified canonical form or an explicit statement that none is established
- [ ] Truth tables pass the harness
- [ ] PARITY versus ExactlyOne versus XOR for three or more operands is explicitly contrasted
- [ ] Diagrams, where present, are semantically identical to the documented formula
- [ ] Every table, formula and canonical form is verified against the independent Strong K3 oracle by the verification harness (ticket 02); relative links resolve
- [ ] Written with the github-markdown skill conventions; Mermaid diagrams (only where they materially help) use the mermaid-diagram-generator skill and are verified to be semantically identical to the documented formula

Source: [spec audit](../../k3-conformance/spec-audit.md) sections A and D. See also [spec](../spec.md).
