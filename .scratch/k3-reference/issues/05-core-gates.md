# 05: Core gates: NOT, AND, OR

**What to build:** One document each for NOT, AND and OR (the primitive Strong K3 connectives) following the approved template: classification, arity (AND and OR variadic with two or more operands in the engine; state the empty and single-operand identity conventions and how the engine treats them), domains, definition, syntax including the symbol and word forms, aliases, formal semantics and formula, exhaustive truth tables, examples with Unknown, edge cases (n-ary evaluation, short-circuit does not change the value, faults as Unknown) and related operations. A diagram only if it adds information.

**Blocked by:** 01, 02, 03

**Status:** ready-for-agent

- [ ] Three operation documents conform to the approved template
- [ ] Exhaustive tables for NOT, AND and OR (up to 4 operands for the variadic ones) pass the harness
- [ ] Unknown cases and the F-dominates-AND and T-dominates-OR behaviour are explicit
- [ ] Aliases are only those the engine actually accepts
- [ ] Every table, formula and canonical form is verified against the independent Strong K3 oracle by the verification harness (ticket 02); relative links resolve
- [ ] Written with the github-markdown skill conventions; Mermaid diagrams (only where they materially help) use the mermaid-diagram-generator skill and are verified to be semantically identical to the documented formula

Source: [spec audit](../../k3-conformance/spec-audit.md) section A. See also [spec](../spec.md).
