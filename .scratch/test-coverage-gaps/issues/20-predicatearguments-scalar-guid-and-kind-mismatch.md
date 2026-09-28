# 20: Cover PredicateArguments' GetGuid and kind-mismatch throw

**What to build:** `PredicateArguments.GetGuid` (src/TruthWeaver.Abstractions/PredicateArguments.cs, line ~65) and the private `Get` helper's kind-mismatch `InvalidOperationException` (lines ~132-134) have no test coverage per the ticket 11 Cobertura report. `Get` backs every typed accessor on this class, so its mismatch throw is exercised indirectly by nothing today — a predicate author who requests the wrong kind gets an untested error path at evaluation time.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `GetGuid` returns the expected `Guid` for an argument supplied with `LiteralKind.Guid`.
- [x] Calling any typed getter (e.g. `GetGuid`) against an argument supplied with a different `LiteralKind` throws `InvalidOperationException` naming both the argument and the mismatched kinds in its message.
- [x] Existing `PredicateArguments` tests continue to pass unchanged.
