# 09: Functions and external operators: COALESCE, If, inspections

**What to build:** Documents for COALESCE (alias ?? and n-ary form; replaces only Unknown; associative), If (alias ? :; the consensus term so that If(Unknown, A, A) = A, the strongest extension of if-then-else, contrasted with the bare multiplexer, McCarthy's conditional and SQL CASE), and IsTrue, IsFalse, IsUnknown and IsKnown (definite results, SQL IS TRUE / IS UNKNOWN precedents). Each is marked as an external operator rather than a Strong K3 connective (not information-monotone), with the classification ambiguity recorded as an open question where the owner has not decided the category. A decision-flow diagram for If if it helps.

**Blocked by:** 05

**Status:** ready-for-agent

- [ ] Documents conform to the template; the external-operator caveat is stated in each
- [ ] If has a 27-triple evaluation table verified by the harness; the SQL CASE equivalence is documented
- [ ] COALESCE associativity is verified, not asserted
- [ ] Classification ambiguities appear in the unresolved-questions list
- [ ] Every table, formula and canonical form is verified against the independent Strong K3 oracle by the verification harness (ticket 02); relative links resolve
- [ ] Written with the github-markdown skill conventions; Mermaid diagrams (only where they materially help) use the mermaid-diagram-generator skill and are verified to be semantically identical to the documented formula

Source: [spec audit](../../k3-conformance/spec-audit.md) section C. See also [spec](../spec.md).
