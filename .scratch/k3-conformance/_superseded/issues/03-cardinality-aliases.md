# 03: Cardinality aliases and derived-operator docs

**Status:** ready-for-agent after 02
**Blocked by:** 02

**What to build:** `ANY`, `ALL`, `NONE`, `BETWEEN(min, max, ...)` as derived nodes over the threshold primitives, using the `[definitely true, possibly true]` interval.

- [ ] Each alias equals its primitive definition over the oracle for all inputs (n up to 4)
- [ ] Parse, print, JSON/YAML, builder, descriptors, analyzer support
- [ ] `BETWEEN` argument validation (`min <= max`, range vs operand count) with diagnostics
- [ ] Rewrite CONTEXT.md "Equivalency rules" section: derived-operator definition table replaces the "no All/None" rationale; update `equivalency-docs` ticket cross-reference
