# 07: Cardinality primitives

**What to build:** Documents for AtLeast, AtMost, Exactly, GreaterThan, LessThan and ExactlyOne, all defined by the definitely-true / possibly-true interval [T, T+U] over the true-count. Each has an Evaluation Table (parameterized and variadic, so a truth table is impractical) with the complete semantic definition, representative cases, boundary cases, the valid range of k and what is rejected, Unknown cases, empty and single-operand behaviour, and the reduction of AtLeast(1) to OR and AtLeast(n) to AND where it holds. A single interval diagram if it materially helps.

**Blocked by:** 05

**Status:** ready-for-agent

- [ ] Six documents conform to the template with evaluation tables that pass the harness
- [ ] Valid k ranges match the engine's compile-time rules
- [ ] AtLeast(1) = OR and AtLeast(n) = AND are verified before being documented
- [ ] The difference between ExactlyOne and PARITY is cross-linked
- [ ] Every table, formula and canonical form is verified against the independent Strong K3 oracle by the verification harness (ticket 02); relative links resolve
- [ ] Written with the github-markdown skill conventions; Mermaid diagrams (only where they materially help) use the mermaid-diagram-generator skill and are verified to be semantically identical to the documented formula

Source: [spec audit](../../k3-conformance/spec-audit.md) section A and appendix E.9. See also [spec](../spec.md).
