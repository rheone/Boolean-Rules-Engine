# 18: Cover TermIdentity's null-handling, reference-equality fast path, and `==`/`!=` operators

**What to build:** `TermIdentity.Equals(TermIdentity?)`'s `null` short-circuit (line ~47) and `ReferenceEquals` fast path (line ~52), plus the `==`/`!=` operators (lines ~30, ~39) that delegate to the static `Equals` helper, have no test coverage per the ticket 11 Cobertura report. `TermIdentity` is, per its own doc comment, "the key used for per-evaluation memoization" — a bug in its null-handling or reference-equality shortcut would silently corrupt memoization lookups rather than fail loudly.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `identity.Equals(null)` returns `false`.
- [x] `identity.Equals(identity)` (same reference) returns `true` without needing element-wise comparison to succeed independently.
- [x] `left == right` and `left != right` are exercised for equal identities, differing-predicate-name identities, and differing-argument identities, including the `null`-on-one-side case.
- [x] Existing `TermIdentity` tests continue to pass unchanged.
