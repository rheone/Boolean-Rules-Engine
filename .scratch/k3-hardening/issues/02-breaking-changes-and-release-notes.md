# 02: Breaking-change notes and release readiness

**What to build:** A changelog and migration guide that lists every public break introduced by the Strong K3 work, so a consumer can upgrade: predicates return TruthValue instead of bool; constants are TruthValue and canonical text is True/False/Unknown instead of lowercase; XnorExpression became EquivalentExpression and the canonical label is EQUIVALENT; the shared infix arity diagnostic constant rename (code unchanged); NXOR became PARITY; Collapse and Project are methods on the result and are no longer rule-language features; Decision.Outcome removed; PrintText became PrintRuleText; JSON/YAML shape changes (new op names, equivalent/xnor/iff reading, collapse/project removal) and the JSON/YAML parsers now reporting a diagnostic instead of throwing for a malformed k. Also verify package versions, NuGet metadata, README install and quick-start sections, and that a pre-1.0 version bump policy is stated.

**Blocked by:** k3-followups 04, k3-followups 05, k3-followups 06

**Status:** ready-for-agent

- [ ] CHANGELOG (or the repository's equivalent) lists each break with the old and new form and a migration step
- [ ] README quick-start compiles against the current API
- [ ] Package metadata and version policy checked and documented
- [ ] The list is cross-checked against the git history and the k3-followups tickets

See also [spec](../spec.md).
