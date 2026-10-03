# 01: K3 oracle and conformance audit

**What to build:** A test-only truth-table oracle that computes the expected K3 result from the primitive definitions (NOT, AND, OR, cardinality definitely-true/possibly-true interval) over all {True, False, Unknown} inputs up to 4 operands, plus an audit of today's operators against it with the gaps recorded.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Oracle helper is reusable by later slices through the public pipeline (text/JSON/YAML -> Compile -> EvaluateAsync -> Decision)
- [x] Existing NOT/AND/OR/XOR/ExactlyOne/threshold operators are verified against the oracle
- [x] Any divergence found is recorded in this ticket's Comments section
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

- Oracle: `tests/TruthWeaver.Tests/TestSupport/K3Oracle.cs`; harness: `K3Rule.cs`; audit: `K3ConformanceTests.cs`.
- Audit result: NOT, AND, OR (2-4 operands), XOR, XNOR, ExactlyOne and the AtLeast/AtMost/GreaterThan/LessThan/Exactly family all match the oracle for every {T,F,U} assignment. No divergences found.
- Unknown inputs are delivered as thrown faults until ticket 02 lets predicates return `TruthValue`; `K3Rule.TryCreate` is the one place to change then.
