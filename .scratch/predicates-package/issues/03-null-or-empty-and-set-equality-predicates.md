# 03: `IsNullOrEmpty` and set-equality predicates

**What to build:** An `IsNullOrEmpty` predicate over a `Func<TContext, string?>` selector, and an order-insensitive set-equality predicate over a `Func<TContext, IReadOnlyCollection<string>>` selector compared against an array-literal argument.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] `IsNullOrEmpty` predicate: true when the selected string is `null` or `""`
- [ ] Set-equality predicate: true when the selected collection and the literal array argument contain the same elements, ignoring order and duplicates (set semantics, not sequence semantics) — this is a deliberate divergence from CONTEXT.md's "array-valued arguments are order-sensitive" term-identity rule, which governs *term identity* (two terms are the same variable), not this predicate's *evaluation* semantics; call this out explicitly in the predicate's `Description` so it isn't read as contradicting that rule
- [ ] Case sensitivity for the set-equality predicate defaults to case-sensitive (consistent with CONTEXT.md's general "argument values are case-sensitive" stance); decide and document whether a case-insensitive variant/argument is in scope for this ticket or deferred
- [ ] Tests: equal sets given in different orders match; a set with a differing case matches or doesn't per the decision above; empty-selected-collection vs. empty-literal-array matches; `null` selected collection is treated as empty (not a fault) — document this choice in the `Description`
