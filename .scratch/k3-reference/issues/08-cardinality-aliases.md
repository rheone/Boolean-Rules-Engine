# 08: Cardinality aliases: ANY, ALL, NONE, BETWEEN

**What to build:** Documents for the derived cardinality operations with canonical forms: ANY = AtLeast(1), ALL = AtLeast(n), NONE = AtMost(0), BETWEEN(min, max, ...) = AND(AtLeast(min), AtMost(max)). Each states the engine's minimum operand count and the BETWEEN rules (0 <= min <= max <= n, the full range is rejected as a constant), the K3 coincidences with OR, AND and NOT-OR (and why they remain distinct operations), the correct All() behaviour (False only when T+U < n) and any caveat found by the audit.

**Blocked by:** 07

**Status:** ready-for-agent

- [ ] Four documents conform to the template with verified canonical forms
- [ ] The corrected ALL behaviour is documented and checked by the harness
- [ ] BETWEEN bound rules and the empty-range behaviour are explicit
- [ ] Related-operation links to ticket 07 documents resolve
- [ ] Every table, formula and canonical form is verified against the independent Strong K3 oracle by the verification harness (ticket 02); relative links resolve
- [ ] Written with the github-markdown skill conventions; Mermaid diagrams (only where they materially help) use the mermaid-diagram-generator skill and are verified to be semantically identical to the documented formula

Source: [spec audit](../../k3-conformance/spec-audit.md) B2, B6. See also [spec](../spec.md).
