# 14: Keep the reference in sync as predicates (and operations) are added

**What to build:** A guard and a process so the reference library at docs/strong-k3/ never falls behind the engine. A check (run with the other gates, built on the verification harness from ticket 02) fails when a built-in predicate registered by the predicates package, or an operator known to the engine's operator tables, has no reference document, or when a reference document names a predicate or operator the engine does not have. Until the predicates are documented (ticket 13), the check reports predicates as "on hold" through an explicit, visible allow-list rather than silently skipping them; releasing the hold in ticket 13 empties the allow-list. Add the working rule to the places contributors and agents read: any ticket that adds, renames or removes a predicate or operation must add, rename or remove its reference document, update the category index and the root navigation, and re-run the harness (a line in CLAUDE.md, the ticket template notes and the reference's own README). Each newly added predicate gets its document following the approved template (Name, Classification, Kind, Arity, Input and Output Domain, Definition, Syntax, Aliases, Formal Semantics, plus the conditional sections that apply), including its null-selected-value behaviour and its Unknown cases.

**Blocked by:** 02, 04 (the guard itself); full predicate coverage additionally waits for 13 and for each predicate's own implementation ticket

**Status:** ready-for-agent

- [ ] The sync check fails when a registered built-in predicate or a known operator has no reference document, and when a document refers to something the engine lacks (demonstrated with a fixture or a temporary example)
- [ ] The predicate allow-list is explicit, committed and referenced from the check's failure message; ticket 13 removes it
- [ ] CLAUDE.md, the reference README and the ticket workflow notes state the rule that adding or changing a predicate or operation updates its reference document in the same change
- [ ] Future predicate tickets are given an acceptance criterion pointing at this rule (add it to the predicate catalog track's ticket notes)
- [ ] The check is wired into the same gates as the rest of the validation set and documented
- [ ] Built test-first; the full validation set in CLAUDE.md passes

Source: owner request 2026-10-03 (keep the document in sync with additionally added predicates). See also [spec](../spec.md) and [predicate gap list](../../predicate-catalog/k3-gap-list.md).
