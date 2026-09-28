# 12: Cover JsonTreePrinter literal-kind printing

**What to build:** `JsonTreePrinter.LiteralToNode` (src/TruthWeaver/Json/JsonTreePrinter.cs) switches
on every `LiteralKind` when printing a term's argument to JSON, but the Cobertura report from ticket
11 shows only the `String` and `Int64` branches are exercised — `Decimal`, `Boolean`, `Guid`,
`DateTimeOffset`, and the `ArrayLiteralToNode` fallback (lines ~79-100) are never hit by any test.
Verify each of those literal kinds prints correctly to the flat JSON tree shape (ADR-0003) when a
compiled rule with that argument kind is printed via `CompiledRule.PrintJson()`.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] A rule with a `Decimal` predicate argument prints its value as a JSON number via `PrintJson()`.
- [x] A rule with a `Boolean` predicate argument prints its value as a JSON boolean.
- [x] A rule with a `Guid` predicate argument prints its value as a JSON string (via `ToString()`).
- [x] A rule with a `DateTimeOffset` predicate argument prints its value as a round-trippable (`"O"` format) JSON string.
- [x] A rule with an array-kind predicate argument (e.g. `Int64Array` or `BooleanArray`) prints as a JSON array via `ArrayLiteralToNode`.
- [x] Existing JSON tree printer/round-trip tests continue to pass unchanged.

## Comments

Added five tests to `tests/TruthWeaver.Tests/JsonTreeTests.cs`, each compiling a small DSL rule
through a predicate with the target argument kind, printing it via `CompiledRule.PrintJson()`, and
asserting on the raw `JsonElement` shape (not just round-trip equality) so each `LiteralToNode`
branch is directly exercised:

- `A_decimal_argument_prints_as_a_json_number` — `LiteralKind.Decimal` branch.
- `A_boolean_argument_prints_as_a_json_boolean` — `LiteralKind.Boolean` branch.
- `A_guid_argument_prints_as_a_json_string_via_its_ToString` — `LiteralKind.Guid` branch.
- `A_datetimeoffset_argument_prints_as_a_round_trippable_o_format_json_string` — `LiteralKind.DateTimeOffset` branch.
- `An_array_kind_argument_prints_as_a_json_array_via_ArrayLiteralToNode` — the `Int64Array` fallback into `ArrayLiteralToNode`.

Also added `AddDecimalArgPredicate`/`AddBooleanArgPredicate` helpers to
`tests/TruthWeaver.Tests/TestSupport/TestPredicates.cs`, matching the existing
`AddGuidArgPredicate`/`AddDateTimeOffsetArgPredicate` style, since no decimal/boolean single-argument
test predicate existed yet. All five new tests and the 28 pre-existing `JsonTreeTests` pass (35/35).
