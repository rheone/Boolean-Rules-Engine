# 13: Correct the .tmp reference documents

**What to build:** Edit the wrong passages in the .tmp reference documents in place, adding a short 'Corrected 2026-10-03, see spec-audit.md' note beside each fix (owner decision). Fixes: the two consensus-theorem passages (the three-term form is not reducible in K3; keep the catalog rule that forbids it); the All() row of the cardinality summary (False when T+U<n, not T<n); ProjectAndCollapse.md (self-contradictory; replace with a short note that Project and Collapse are methods on the result, per the final design); the 'inverse coalesce' example (it is COALESCE itself; give a real counter-example or retitle); the grammar-versus-precedence conflict for XOR/AND (align to the table, which matches C#); BETWEEN requires min <= max; the 'major three-valued systems' table (Kleene and Strong Kleene are one logic, add weak Kleene, correct McCarthy and Lukasiewicz notes); NXOR renamed PARITY. Spec audit sections B1-B7.

**Blocked by:** 04, 05, 06

**Status:** ready-for-agent

- [ ] Every audit item B1-B7 is corrected with a dated note and a link to the audit
- [ ] The brute-force script's relevant checks (spec audit appendix E) agree with the corrected text
- [ ] No code files are touched

Source: [spec audit](../../k3-conformance/spec-audit.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
