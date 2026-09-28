# 15: Cover OperatorInfo defensive exception branches

**What to build:** `OperatorInfo.Describe` (src/TruthWeaver/Ast/OperatorInfo.cs) throws
`ArgumentException` when called with a `TermExpression` (lines ~26-31), since a term's label/
description come from its predicate schema instead — and `ThresholdDescription`'s `default` branch
throws `InvalidOperationException` for an unhandled `ThresholdComparison` (line ~62), defensive against
the closed set (ADR-0004) ever growing without updating this switch. Neither branch has any test
coverage per the ticket 11 Cobertura report.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Calling `OperatorInfo.Describe` with a `TermExpression` throws `ArgumentException` naming the `node` parameter, with a message pointing callers at the predicate schema instead.
- [x] The `ThresholdDescription` unhandled-comparison branch is exercised (directly, or documented as unreachable through the public API with a rationale, matching the precedent set by ticket 08's `SimulatedPredicateFaultException` coverage for similar defensive code).
- [x] Existing `OperatorInfo`/tree-rendering tests continue to pass unchanged.

## Comments

Added two tests to `tests/TruthWeaver.Tests/OperatorInfoTests.cs`:

- `Describe_throws_for_a_term_expression_pointing_callers_at_the_predicate_schema_instead` — calls
  `OperatorInfo.Describe` with a bare `TermExpression`, asserting the thrown `ArgumentException`'s
  `ParamName` is `"node"` and its message mentions `PredicateSchema`.
- `ThresholdDescriptions_default_branch_throws_for_an_unhandled_comparison_name` — `ThresholdComparison`'s
  five values (ADR-0004's closed set) are all handled by `ThresholdDescription`'s switch, so its
  `default` arm is genuinely unreachable through the public API today (`ExpressionShape.Of` always
  derives `NodeShape.OpName` from `ThresholdComparison.ToString()`, which can only ever be one of the
  five enum names). Went with the "exercised directly" option from the checklist rather than
  documenting it as untested: since `TruthWeaver.Tests` already has `InternalsVisibleTo` access to
  `NodeShape` (an internal type), the private `ThresholdDescription` method is invoked directly via
  reflection with a hand-built `NodeShape("Bogus", 1, [])`, asserting it throws
  `InvalidOperationException` with a message containing "Unhandled threshold comparison" — directly
  covering the `default` branch itself, the same rationale ticket 08 used for its otherwise-unreachable
  exception-constructor coverage.

All 19 `OperatorInfoTests` (2 new + 17 pre-existing) pass.
