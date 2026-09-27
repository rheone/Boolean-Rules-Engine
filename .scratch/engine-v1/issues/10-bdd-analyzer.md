# 10: BDD-based analyzer: constant/contradiction/redundancy diagnostics

**What to build:** A BDD-based (binary decision diagram) analyzer that runs during compilation (see the pipeline in ADR-0003: Parse → Validate → Analyze → Build) and flags sub-expressions that are structurally always-true, always-false, or contradictory as compile diagnostics — e.g. `hasRole(role: "Y") AND NOT hasRole(role: "Y")` should be flagged, using term identity (ticket 04) to recognize the two `hasRole(role: "Y")` references as the same variable. This analysis respects the term-count cap from ticket 09 and is skipped (with an `Info` diagnostic) beyond it.

This ticket is flagged in the spec as the one component least likely to be a direct ADR-to-code translation — treat the BDD construction and variable-ordering approach as a design decision to make explicitly (and document, if it materially shapes the result) rather than something to reverse-engineer from the truth tables alone.

**Blocked by:** 05

**Status:** done (verified against existing codebase — already implemented prior to this pass)

- [ ] `hasRole(role: "Y") AND NOT hasRole(role: "Y")` produces a diagnostic identifying the sub-expression as a structural contradiction (always `False`), using term identity to recognize both references as the same variable
- [ ] `hasRole(role: "Y") OR NOT hasRole(role: "Y")` produces a diagnostic identifying the sub-expression as a structural tautology (always `True`)
- [ ] A rule with no constant/contradictory sub-expression produces no such diagnostics
- [ ] Analysis correctly incorporates every operator from tickets 03–05 (`AND`, `OR`, `NOT`, `XOR`, `ExactlyOne`, `AtLeast`), not just `AND`/`OR`/`NOT`
- [ ] A rule whose distinct-term count exceeds the `CompilerOptions` cap from ticket 09 skips this analysis and reports it as an `Info` diagnostic, without hanging or silently claiming the rule is "not constant"
- [ ] These diagnostics are `Warning` or `Info` severity, not `Error` — they don't block compilation or persistence
