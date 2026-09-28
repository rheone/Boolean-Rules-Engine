# 21: Cover PredicateArguments' array-typed getters

**What to build:** `PredicateArguments.GetStringArray`, `GetInt64Array`, `GetDecimalArray`, `GetBoolArray`, `GetDateTimeOffsetArray`, and `GetGuidArray` (src/TruthWeaver.Abstractions/PredicateArguments.cs, lines ~73-113) have no test coverage per the ticket 11 Cobertura report. These are the public API every custom predicate calls to read its array-valued arguments during evaluation — an untested element-conversion bug here would surface as a wrong evaluated value at runtime, not a compile error. [[20-predicatearguments-scalar-guid-and-kind-mismatch]] covers the scalar/mismatch side of the same class.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Each of `GetStringArray`, `GetInt64Array`, `GetDecimalArray`, `GetBoolArray`, `GetDateTimeOffsetArray`, and `GetGuidArray` returns the expected element values, in order, for an argument supplied with the matching array `LiteralKind`.
- [x] At least one array getter is exercised against an empty array argument.
- [x] Existing `PredicateArguments` tests continue to pass unchanged.
