# 02: Cross-reference the pattern from CONTEXT.md and the context-bound-term-arguments ticket

**What to build:** `CONTEXT.md`'s `Deferred` table already has a one-line
justification for "Context-bound term arguments" ("a predicate that needs a
context value reaches into the context itself (`IsManagerOfResourceOwner`)")
but that line only gestures at the alternative without naming it as a
distinct, documented pattern. Once ticket 01 lands a real README section for
it, this ticket wires the cross-reference both ways.

**Blocked by:** ticket 01 (needs the README section/heading to link to)

**Status:** done

- [x] `CONTEXT.md`'s `Deferred` table row for "Context-bound term arguments"
  is updated to link to README's new subsection (from ticket 01) by name,
  replacing or extending the current parenthetical example so it reads as
  "see \[pattern name\] for the documented, tested alternative" rather than
  a bare inline example.
- [x] The row's wording is corrected to not imply the alternative is
  specifically a *relationship* between two parties, or that "the context
  value" must be an identity or "the current user" — match `spec.md`'s
  broadened framing: this is a general externally-resolved-value pattern
  (single-value, single-sided, or two-sided), of which relationship
  comparison is only one instance.
- [x] `.scratch/context-bound-term-arguments/issues/01-investigate-and-plan-context-bound-term-arguments.md`'s
  `## Comments` section gets a short appended note (new subsection or
  final paragraph, not a rewrite of the existing recommendation) pointing
  to this pattern's README section and this ticket set, so a future reader
  of that investigation finds the concrete alternative that was actually
  implemented and tested, not just referenced in passing.
- [x] No change to that ticket's `**Status:**` line (it stays `done`) and no
  change to its actual recommendation — this is a pointer addition, not a
  reopening.
- [x] No production code changes.

## Comments

Updated `CONTEXT.md`'s `Deferred` table row for "Context-bound term
arguments" to link to README's ["n arguments, class-based,
externally-resolved value"](../../../README.md#n-arguments-class-based-externally-resolved-value)
section (the heading ticket 01 actually landed) and reworded it to describe
a general live-resolved-value pattern — keyed by a rule-text literal, a
`TContext`-supplied value, or both — rather than implying a relationship
between two parties or that either side must be an identity/"the current
user." Appended an addendum to
`.scratch/context-bound-term-arguments/issues/01-investigate-and-plan-context-bound-term-arguments.md`'s
`## Comments` pointing at the same README section and this ticket set,
without touching its `**Status:** done` line or its "remain deferred"
recommendation. No production code changed; `dotnet build`, `dotnet test`,
`dotnet csharpier check .`, and `dotnet format --verify-no-changes
--severity info` all pass.
