# 31: Cover RuleNodeCompiler's ErrorNode placeholder and remaining defensive throws

**What to build:** `RuleNodeCompiler<TContext>.Build`'s `ErrorNode` case (src/TruthWeaver/Compilation/RuleNodeCompiler.cs, line ~116, which substitutes `ConstantExpression(false)` for a node the parser already flagged as malformed) has no test coverage, merged across all six Cobertura reports from ticket 11's coverage run — this is distinct from the diagnostics ticket 14 already covered. `Build`'s unhandled-node-type `default` throw (line ~143) and `ValidThresholdRange`'s unhandled-comparison `default` throw (line ~77) remain untested too; both are defensive against their respective closed sets (`RuleNode` subtypes, `ThresholdComparison`) growing without this switch being updated, the same shape as ticket 15's `OperatorInfo` precedent.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Compiling a tree containing an `ErrorNode` (e.g. produced by a parser that recovered from a malformed subtree) substitutes `ConstantExpression(false)` at that position without throwing, and compilation of the surrounding tree still succeeds.
- [x] `Build`'s unhandled-`RuleNode`-type `default` branch and `ValidThresholdRange`'s unhandled-`ThresholdComparison` `default` branch are each either exercised directly (e.g. via internals access with a hand-built unrecognized case, matching ticket 08/15's precedent) or documented as unreachable through the public API with the same rationale.
- [x] Existing `RuleNodeCompiler` tests continue to pass unchanged.

## Comments

Added `tests/TruthWeaver.Tests/RuleNodeCompilerErrorNodeAndDefensiveThrowsTests.cs` with three tests,
all calling the internal `RuleNodeCompiler<TContext>.Compile` directly (available via
`InternalsVisibleTo`) rather than through `RuleCompiler`'s public DSL/JSON/YAML entry points:

- `An_error_node_compiles_to_a_false_constant_without_throwing_and_the_surrounding_tree_still_compiles`
  — hand-builds an `AndNode` with one `ConstantNode(true)` operand and one `ErrorNode` operand and
  calls `RuleNodeCompiler.Compile` directly. Asserts it returns a non-null `AndExpression` whose
  operands are `[ConstantExpression(true), ConstantExpression(false)]` with zero diagnostics, proving
  the substitution happens silently and the surrounding tree still compiles successfully.

  Going through the public API isn't possible for this one: the DSL parser only ever produces an
  `ErrorNode` alongside a `SyntaxError` diagnostic of `DiagnosticSeverity.Error`
  (`src/TruthWeaver/Parsing/DslParser.cs` line ~269), and `RuleCompiler.CompileNode`
  (`src/TruthWeaver/Compilation/RuleCompiler.cs` lines 108-113) short-circuits before ever calling
  `RuleNodeCompiler.Build` whenever any front-end diagnostic is already an error. So the `ErrorNode`
  case in `Build` is genuinely unreachable through `RuleCompiler.Compile`/`CompileJson` today; the test
  above reaches it directly via internals access instead, the same "exercised directly" option ticket
  08/15 used for similarly unreachable defensive code.

- `Builds_unhandled_rule_node_type_default_branch_throws_naming_the_offending_type` — defines a
  test-only `BogusRuleNode : RuleNode` record (outside the closed set `Build`'s switch handles) and
  asserts `RuleNodeCompiler.Compile` throws `InvalidOperationException` naming both "Unhandled rule
  node type" and the bogus type's name. No production front end can ever produce a `RuleNode` subtype
  this switch doesn't handle, so this branch is unreachable through any public front end; exercised
  directly the same way, via a hand-built subtype rather than reflection, since `RuleNode` is merely
  `internal` (not `sealed`) and the test assembly already has `InternalsVisibleTo` access to derive
  from it.

- `ValidThresholdRanges_unhandled_comparison_default_branch_throws_naming_the_bogus_value` — hand-builds
  a `ThresholdNode` with `(ThresholdComparison)999` (outside the five-value closed set, ADR-0004) and
  asserts `RuleNodeCompiler.Compile` throws `InvalidOperationException` containing "Unhandled threshold
  comparison". Unlike ticket 15's `OperatorInfo.ThresholdDescription` (whose discriminant is derived
  from `ThresholdComparison.ToString()` and so needed reflection to force a bogus value), this switch's
  discriminant is the `Comparison` carried directly on a `ThresholdNode`, so the bogus value reaches it
  through ordinary construction — no reflection needed.

All 700 tests pass (697 pre-existing + 3 new). `dotnet csharpier check .` and
`dotnet format --verify-no-changes --severity info` both pass clean.
