# 10: Result transformations: Project and Collapse

**What to build:** Documents for Project and Collapse as methods on an already-evaluated result (the original three-valued result is always preserved): Project maps Unknown to a chosen definite value and passes True and False through; Collapse applies UnknownAsFalse, UnknownAsTrue or UnknownIsError (a rejected-unresolved outcome, not a fault). Include the mapping tables, relation to the inspections and to SQL WHERE / CHECK, the fail-closed rule for IsSatisfied, and a mapping diagram. State plainly that these are TruthWeaver terms, not Strong K3 literature terms, and that inside a rule COALESCE(x, True|False) gives the Project effect.

**Blocked by:** 04, k3-followups 04, k3-followups 05

**Status:** ready-for-agent

- [ ] Two documents conform to the template with mapping tables verified against the oracle
- [ ] The documents describe the final method-on-the-result design, matching the code once the follow-up tickets have landed
- [ ] The terminology note and the SQL precedents are cited
- [ ] The original result remaining available is explicit
- [ ] Every table, formula and canonical form is verified against the independent Strong K3 oracle by the verification harness (ticket 02); relative links resolve
- [ ] Written with the github-markdown skill conventions; Mermaid diagrams (only where they materially help) use the mermaid-diagram-generator skill and are verified to be semantically identical to the documented formula

Source: [research findings](../../k3-conformance/research-findings.md) item 1. See also [spec](../spec.md).
