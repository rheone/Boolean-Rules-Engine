# 27: Cover LiteralValue's GetHashCode per-kind branches

**What to build:** `LiteralValue.GetHashCode`'s switch over `Kind` (src/TruthWeaver.Abstractions/LiteralValue.cs, lines ~265-274) has no test coverage for any branch, merged across all six Cobertura reports from ticket 11's coverage run — `GetHashCode` itself is never called anywhere in the current test suite. Lower priority than the equality gaps ([[26-literalvalue-equality-operators-and-mismatch]]) since a wrong hash doesn't cause incorrect results by itself, but it does risk silent dictionary/hash-set misbehavior (e.g. `LiteralValue`-keyed lookups, or hash-based memoization built on top of `TermIdentity`) if it and `Equals` ever drift out of sync.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `GetHashCode()` is called directly (or via placing a `LiteralValue` in a `HashSet<LiteralValue>`/`Dictionary<LiteralValue,_>`) for at least one value of each `LiteralKind` (`String`, `Int64`, `Decimal`, `Boolean`, `DateTimeOffset`, `Guid`, and an array kind), asserting it doesn't throw and that two equal values (per `Equals`) produce the same hash.
- [x] Existing `LiteralValue` tests continue to pass unchanged.
