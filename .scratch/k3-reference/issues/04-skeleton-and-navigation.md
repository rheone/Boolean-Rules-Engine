# 04: Skeleton and navigation

**What to build:** The approved directory tree with an index README.md in every meaningful directory and the root README.md giving an overview and navigation to the specification, values, semantics, Gates / Operators, Predicates, Functions, Cardinality Functions, Derived Operations and Result Transformations. Indexes link to operations without duplicating their definitions. The predicates index is a placeholder stating predicate documentation is on hold until the predicates are implemented (see ticket 13).

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] Every directory in the approved tree has an index with relative links to its members (links to not-yet-written operation files may be listed as pending until their tickets land)
- [ ] Root README navigation reaches every category
- [ ] The harness link check passes
- [ ] No substantive operation definition is duplicated in an index
- [ ] Every table, formula and canonical form is verified against the independent Strong K3 oracle by the verification harness (ticket 02); relative links resolve
- [ ] Written with the github-markdown skill conventions; Mermaid diagrams (only where they materially help) use the mermaid-diagram-generator skill and are verified to be semantically identical to the documented formula

Source: Phases 3 and 7 of the brief. See also [spec](../spec.md).
