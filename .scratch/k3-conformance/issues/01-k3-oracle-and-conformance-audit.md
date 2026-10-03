# 01: K3 oracle and conformance audit

**What to build:** A test-only truth-table oracle that computes the expected K3 result from the primitive definitions (NOT, AND, OR, cardinality definitely-true/possibly-true interval) over all {True, False, Unknown} inputs up to 4 operands, plus an audit of today's operators against it with the gaps recorded.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Oracle helper is reusable by later slices through the public pipeline (text/JSON/YAML -> Compile -> EvaluateAsync -> Decision)
- [ ] Existing NOT/AND/OR/XOR/ExactlyOne/threshold operators are verified against the oracle
- [ ] Any divergence found is recorded in this ticket's Comments section
- [ ] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
