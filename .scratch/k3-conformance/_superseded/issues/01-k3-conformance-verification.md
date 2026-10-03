# 01: K3 verification and conformance

**Status:** ready-for-agent
**Blocked by:** none

**What to build:** Prove the engine is Strong K3 and close gaps. Build a reusable truth-table oracle in tests from the primitive definitions (`NOT`, `AND`, `OR`, cardinality interval) and compare every existing operator against it over all `{T,F,U}` inputs.

- [ ] Exhaustive truth-table tests for `NOT`, `AND`, `OR`, `XOR`, `XNOR`, `ExactlyOne`, and the threshold family (n up to 4)
- [ ] Audit every predicate (`TruthWeaver.Predicates`, `IPredicate`, resolved-value predicates) returns `TruthValue`; faults map to `Unknown`; add conversion where a bool or nullable leaks through
- [ ] No implicit `Unknown`→bool conversion anywhere in public API (test + architecture test)
- [ ] `TruthValue` ordering is not exposed as a numeric/`IComparable` truth ordering (or documented and restricted to internal bounds)
- [ ] Write the failing-test list for any gaps found; fix them
