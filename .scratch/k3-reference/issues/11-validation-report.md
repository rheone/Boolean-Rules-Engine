# 11: Validation report and unresolved questions

**What to build:** Run the full Phase 8 validation and produce the report: Strong K3 semantics, Unknown propagation, multi-arity and variadic behaviour, cardinality, XOR and PARITY, coalescing, derived equivalences, projection and collapse; plus documentation validation (one primary category per operation, arity and domains present, Kind explicit, formulas agree with tables, canonical forms valid, Mermaid agrees with formulas, aliases unambiguous, links valid, no operation documented inconsistently, terminology consistent). Produce the list of missing operations and categories, ambiguous semantics and classifications, unverified equivalences and decisions needed in the underlying specification. Do not invent semantics to fill gaps.

**Blocked by:** 05, 06, 07, 08, 09, 10

**Status:** ready-for-agent

- [ ] docs/strong-k3/VALIDATION.md contains the report with pass/fail per check and evidence
- [ ] The unresolved semantic and classification questions are listed with a recommended resolution each
- [ ] The harness passes on the full reference
- [ ] Nothing was silently resolved: every gap is listed
- [ ] Every table, formula and canonical form is verified against the independent Strong K3 oracle by the verification harness (ticket 02); relative links resolve
- [ ] Written with the github-markdown skill conventions; Mermaid diagrams (only where they materially help) use the mermaid-diagram-generator skill and are verified to be semantically identical to the documented formula

Source: Phase 8 of the brief. See also [spec](../spec.md).
