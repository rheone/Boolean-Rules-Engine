# 13: Cover RuleBuilder literal argument value types

**What to build:** `RuleBuilder.ValueToNode` (src/TruthWeaver/Building/RuleBuilder.cs, lines ~166-191)
converts a fluent-API predicate argument value (`bool`, `int`, `long`, `double`, `decimal`,
`DateTimeOffset`, an `IEnumerable<object>` array, or an unsupported type) to a JSON node before
compiling. The Cobertura report from ticket 11 shows only the `string` branch is exercised by any
test — every other branch, including the `ArgumentException` thrown for an unsupported value type, is
untested.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Building a rule with a `bool`, `int`, `long`, `double`, and `decimal` predicate argument (via the
      fluent `RuleBuilder` API, not DSL/JSON/YAML text) compiles and evaluates correctly for each type.
- [x] Building a rule with a `DateTimeOffset` predicate argument compiles and evaluates correctly.
- [x] Building a rule with an array (`IEnumerable<object>`) predicate argument compiles and evaluates correctly via `ArrayToNode`.
- [x] Supplying an unsupported argument value type (e.g. a custom class instance) throws `ArgumentException` naming the unsupported type.
- [x] Existing `RuleBuilder` tests continue to pass unchanged.

## Comments

Added nine tests to `tests/TruthWeaver.Tests/RuleBuilderTests.cs`, each building a term via
`RuleBuilder.Predicate(name, (argName, value))` with a value of the target CLR type, compiling it
through a real `RuleCompiler`, and evaluating it — proving both that `ValueToNode` converts the value
correctly and that the resulting term round-trips through the full Validate/Analyze/Build pipeline:

- `Predicate_builder_with_a_bool_argument_value_...` — `bool` branch.
- `Predicate_builder_with_an_int_argument_value_...` — `int` branch.
- `Predicate_builder_with_a_long_argument_value_...` — `long` branch.
- `Predicate_builder_with_a_double_argument_value_...` — `double` branch.
- `Predicate_builder_with_a_decimal_argument_value_...` — `decimal` branch.
- `Predicate_builder_with_a_datetimeoffset_argument_value_...` — `DateTimeOffset` branch.
- `Predicate_builder_with_an_array_argument_value_...via_ArrayToNode` — the `IEnumerable<object>` branch.
- `An_unsupported_argument_value_type_throws_ArgumentException_naming_the_type` — the `_ => throw`
  fallback branch, asserting both the message names the unsupported CLR type and `ParamName` is
  `"value"`.

All nine new tests and the 21 pre-existing `RuleBuilderTests` pass (30/30).
