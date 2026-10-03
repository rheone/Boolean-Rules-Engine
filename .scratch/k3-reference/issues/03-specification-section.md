# 03: Specification section: values, semantics, terminology, notation

**What to build:** The docs/strong-k3/specification/ documents that every operation document links to: values (T, F, U and the truth order F < U < T, with the caution that it is an implementation aid and not a numeric ordering, plus the information order), semantics (Strong Kleene connectives as min/max/negation, truth-functional evaluation versus strongest extension, which classical laws fail such as excluded middle and non-contradiction, which hold such as De Morgan, absorption and double negation, and the invalid consensus removal, plus the strong-K3-connective versus external-operator distinction), terminology (Operation, primary category, primitive, derived, canonical form, public form) and notation (the LaTeX conventions used in every formula). Every claim is verified by brute force.

**Blocked by:** 01, 02

**Status:** ready-for-agent

- [ ] Four documents exist and each states its claims with a verified table or counter-example
- [ ] The strong versus external operator distinction and the information order are documented
- [ ] Notation covers every symbol later documents will use
- [ ] The 'classical laws that fail' list matches the spec audit's verified results
- [ ] Every table, formula and canonical form is verified against the independent Strong K3 oracle by the verification harness (ticket 02); relative links resolve
- [ ] Written with the github-markdown skill conventions; Mermaid diagrams (only where they materially help) use the mermaid-diagram-generator skill and are verified to be semantically identical to the documented formula

Source: [spec audit](../../k3-conformance/spec-audit.md) sections A, C and D. See also [spec](../spec.md).
