# 15: Cover OperatorInfo defensive exception branches

**What to build:** `OperatorInfo.Describe` (src/TruthWeaver/Ast/OperatorInfo.cs) throws
`ArgumentException` when called with a `TermExpression` (lines ~26-31), since a term's label/
description come from its predicate schema instead — and `ThresholdDescription`'s `default` branch
throws `InvalidOperationException` for an unhandled `ThresholdComparison` (line ~62), defensive against
the closed set (ADR-0004) ever growing without updating this switch. Neither branch has any test
coverage per the ticket 11 Cobertura report.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Calling `OperatorInfo.Describe` with a `TermExpression` throws `ArgumentException` naming the `node` parameter, with a message pointing callers at the predicate schema instead.
- [ ] The `ThresholdDescription` unhandled-comparison branch is exercised (directly, or documented as unreachable through the public API with a rationale, matching the precedent set by ticket 08's `SimulatedPredicateFaultException` coverage for similar defensive code).
- [ ] Existing `OperatorInfo`/tree-rendering tests continue to pass unchanged.
