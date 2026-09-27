# 06: Cover RuleBuilder's untested factory methods

**What to build:** `RuleBuilderTests.cs` has 8 tests for a 15-factory-method public API
(`src/BooleanRulesEngine/Building/RuleBuilder.cs`). Grepping the existing test file for each
factory's usage shows these are never exercised at all: `RuleBuilder.Or`, `.ExactlyOne`,
`.AtMost`, `.GreaterThan`, `.LessThan`, `.Exactly`, and `.ToJson()`. (`And`, `Not`, `Xor`, `Xnor`,
and `AtLeast` each have exactly one test — thin, but not zero; leave those as-is unless a
follow-up ticket is filed.)

Follow the existing test file's pattern (build with the fluent API, `Compile` against a test
registry, assert the resulting `Decision` for representative true/false inputs — see the existing
`And`/`Not`/`Xor` tests for the shape to match).

Cover:

- `RuleBuilder.Or(...)` with at least one true operand evaluates true; all-false evaluates false
- `RuleBuilder.ExactlyOne(...)` evaluates true when exactly one operand is true, false for zero or
  more than one (mirrors the DSL-level `ExactlyOneNode` tests in
  `XorExactlyOneThresholdTests.cs`, but via the builder API)
- `RuleBuilder.AtMost(k, ...)`, `.GreaterThan(k, ...)`, `.LessThan(k, ...)`, `.Exactly(k, ...)`
  each evaluate correctly for at least one true and one false case near the boundary `k`
- `RuleBuilder.ToJson()` on a built tree produces JSON that round-trips: parsing it back (via
  whatever the existing JSON front end exposes — see `JsonTreeTests.cs`) reconstructs a
  structurally equivalent tree, or at minimum compiles and evaluates identically to the original

**Blocked by:** none

**Status:** done

- [x] Each bullet above has a corresponding test in `RuleBuilderTests.cs`
- [x] `dotnet test` passes

## Comments

Added theory-based tests to `RuleBuilderTests.cs` for `Or`, `ExactlyOne`, `AtMost`, `GreaterThan`,
`LessThan`, `Exactly`, and a `ToJson()` round-trip test that reparses the emitted JSON, recompiles
it, and asserts identical evaluation to the original tree. `dotnet test` passes (307/307).
