# 12: Cover JsonTreePrinter literal-kind printing

**What to build:** `JsonTreePrinter.LiteralToNode` (src/TruthWeaver/Json/JsonTreePrinter.cs) switches
on every `LiteralKind` when printing a term's argument to JSON, but the Cobertura report from ticket
11 shows only the `String` and `Int64` branches are exercised — `Decimal`, `Boolean`, `Guid`,
`DateTimeOffset`, and the `ArrayLiteralToNode` fallback (lines ~79-100) are never hit by any test.
Verify each of those literal kinds prints correctly to the flat JSON tree shape (ADR-0003) when a
compiled rule with that argument kind is printed via `CompiledRule.PrintJson()`.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] A rule with a `Decimal` predicate argument prints its value as a JSON number via `PrintJson()`.
- [ ] A rule with a `Boolean` predicate argument prints its value as a JSON boolean.
- [ ] A rule with a `Guid` predicate argument prints its value as a JSON string (via `ToString()`).
- [ ] A rule with a `DateTimeOffset` predicate argument prints its value as a round-trippable (`"O"` format) JSON string.
- [ ] A rule with an array-kind predicate argument (e.g. `Int64Array` or `BooleanArray`) prints as a JSON array via `ArrayLiteralToNode`.
- [ ] Existing JSON tree printer/round-trip tests continue to pass unchanged.
