# 13: Cover RuleBuilder literal argument value types

**What to build:** `RuleBuilder.ValueToNode` (src/TruthWeaver/Building/RuleBuilder.cs, lines ~166-191)
converts a fluent-API predicate argument value (`bool`, `int`, `long`, `double`, `decimal`,
`DateTimeOffset`, an `IEnumerable<object>` array, or an unsupported type) to a JSON node before
compiling. The Cobertura report from ticket 11 shows only the `string` branch is exercised by any
test — every other branch, including the `ArgumentException` thrown for an unsupported value type, is
untested.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Building a rule with a `bool`, `int`, `long`, `double`, and `decimal` predicate argument (via the
      fluent `RuleBuilder` API, not DSL/JSON/YAML text) compiles and evaluates correctly for each type.
- [ ] Building a rule with a `DateTimeOffset` predicate argument compiles and evaluates correctly.
- [ ] Building a rule with an array (`IEnumerable<object>`) predicate argument compiles and evaluates correctly via `ArrayToNode`.
- [ ] Supplying an unsupported argument value type (e.g. a custom class instance) throws `ArgumentException` naming the unsupported type.
- [ ] Existing `RuleBuilder` tests continue to pass unchanged.
