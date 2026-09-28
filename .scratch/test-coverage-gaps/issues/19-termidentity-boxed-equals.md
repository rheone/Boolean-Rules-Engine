# 19: Cover TermIdentity's boxed `Equals(object?)`

**What to build:** `TermIdentity.Equals(object?)` (src/TruthWeaver.Abstractions/TermIdentity.cs, line ~62) has no test coverage per the ticket 11 Cobertura report. This is the override used whenever a `TermIdentity` is compared through a non-generic path (e.g. as a dictionary key typed `object`, or via `object.Equals`) — [[18-termidentity-equals-null-and-reference-fastpath]] covers the typed `Equals(TermIdentity?)` overload this delegates to.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `identity.Equals((object)other)` returns `true` for a boxed `TermIdentity` with equal predicate name and arguments.
- [x] `identity.Equals((object)other)` returns `false` for a boxed `TermIdentity` with a different predicate name or arguments.
- [x] `identity.Equals(someUnrelatedObject)` returns `false` for a non-`TermIdentity` object.
- [x] Existing `TermIdentity` tests continue to pass unchanged.
