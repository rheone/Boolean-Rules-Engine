# Spec: Externally-resolved-value predicates

**Status:** ready-for-agent

## Background

`context-bound-term-arguments` ticket 01 (done — see its `## Comments`)
investigated a path-expression grammar (`IsManagerOf({{resource.ownerId}})`)
and recommended **remain deferred**, on the grounds that every motivating
example is already expressible today without engine changes, using a shape
that already exists but has never been named, documented, or tested as a
distinct pattern in its own right. That investigation's examples were all
*relationships* (is-manager-of, is-owner-of, is-delegate-of), which is what
led the first draft of this ticket set to frame the whole pattern as
relationship-shaped. **That framing is too narrow.** Relationship
comparison is one instance of a broader shape, not the shape itself:

> A predicate whose boolean answer depends on **live data resolved through
> a constructor-injected service**, keyed by some combination of a
> rule-text literal argument and/or a `TContext`-supplied anchor.

That's it — there does not need to be "two parties" being compared at all.
Concrete instances, none of them a relationship:

- **Single-value resolution, no comparison target at all:** `IsFeatureEnabled(flagKey: "new-checkout")` —
  an injected `IFeatureFlagService` resolves the literal key directly to a
  live boolean. `TContext` may be entirely unused. There is no "other side"
  to resolve or compare against; the resolved value *is* the answer.
- **Single-sided value check:** `IsWithinBudget(costCenterCode: "CC-100")` —
  the literal key resolves, via an injected service, to a live `Decimal`
  limit; that's compared against a plain value already sitting on
  `TContext` (e.g. a requested purchase amount that needs no resolution of
  its own). Only one side is externally resolved.
- **Two-sided relationship comparison** (the original motivating case): both
  a `TContext`-supplied anchor and the rule-text argument are independently
  resolved through the injected service, and the two *resolved* results are
  compared — e.g. "is the manager of resource R (resolved from a context
  anchor) the same person as candidate U (validated from the argument)."

The common thread across all three is only the injection + live-resolution
part, never "there are two parties" — that's just what the relationship
variant happens to need. Docs and tests for this pattern must present the
single-value and single-sided shapes as equally central, not as footnotes to
the relationship case.

A second generalization already captured correctly in the first draft
still holds and should not be lost: **the rule-text argument need not be an
identity** (a `String` cost-center code is as valid a key as a `Guid`), and
**`TContext` is not privileged as "the side that needs no resolution"** —
in the two-sided case either side, or both, may need it; in the
single-value case `TContext` may not participate at all.

This is valuable enough to a library consumer that it should be a **named,
documented, tested pattern**, not something every host independently
rediscovers — analogous to how `README.md`'s existing "n arguments,
class-based, multiple injected dependencies" section documents
`HasEnoughRecentApprovals`. That existing example is close but not quite
this shape: its two `Int64` arguments (`minCount`, `withinHours`) are
literal *values* consumed directly, not *keys* resolved through a live
lookup into something else. This pattern set is the missing sibling family
of examples — plural, because it now covers three distinct shapes, not one.

## Scope

Seven tickets, serial except where noted, each independently completable:

1. Document the pattern family in `README.md` (the "how do I use this"
   audience) — must present the single-value shape first or with equal
   weight to the relationship shape, not as an afterthought.
2. Cross-reference it from `CONTEXT.md`'s `Deferred` table (the "why don't
   we need context-bound term arguments" audience) and from
   `context-bound-term-arguments` ticket 01's own `## Comments`.
3. A worked-example test for the **two-sided relationship shape**: a `Guid`
   argument identifies one party, a `TContext`-supplied anchor that is
   *not itself the other party* (e.g. a resource id, not a user id) is
   resolved to the second party through the injected service — proving the
   context side, not just the argument side, can require external
   resolution, and that neither side is "the current user" by assumption.
4. A worked-example test for the **single-sided value shape** (a `String`
   or numeric key resolved to a comparison value, e.g. a budget/threshold
   lookup, compared against a plain unresolved `TContext` value) — proves
   the pattern doesn't require both sides to be resolved.
5. A worked-example test for the **single-value, no-comparison-target
   shape** (e.g. a feature-flag-style predicate: the literal key resolves
   directly to the boolean answer; `TContext` may be unused entirely) —
   proves the pattern doesn't require a "relationship" or even a second
   value at all.
6. A worked-example test for the **failure shape**: the injected external
   lookup throws (timeout, connection failure) and evaluation must absorb
   it as a `Fault`/`Unknown` per ADR-0001, not surface an unhandled
   exception — this is the pattern's highest-risk edge (a live external call
   inside predicate evaluation) and applies identically across all three
   shapes above, so one dedicated test is enough; it should use whichever
   of tickets 3–5's test-support predicates already exists.
7. A generic `ResolvedValuePredicates` factory added to
   `TruthWeaver.Predicates` (production code) for the sub-case where the
   thing doing the resolving is safe to capture once at registration (a
   long-lived, thread-safe client), as a lighter-weight complement to the
   class-based path tickets 01–06 document/test. This is the one part of
   the pattern family general enough to ship as reusable library code
   rather than remain a per-host authoring pattern — see its own ticket for
   the reasoning on why this sub-case, and only this sub-case, is
   general-use.

No engine/production-code changes are required by tickets 01–02 (docs only).
Tickets 03–06 are test-only (`TruthWeaver.Tests`), following `/tdd` where a
red-green cycle is meaningful (the fault-absorption case in particular should
be written test-first against the existing `Evaluator` fault-absorption
behavior, not against new production code). Ticket 07 is the one production-
code change in this set (`TruthWeaver.Predicates` + its test project),
also via `/tdd`.

## Non-goals

- Not reopening `context-bound-term-arguments`'s "remain deferred"
  recommendation — this pattern is the reason that recommendation holds, not
  a partial rework of it.
- Not adding a new **domain** predicate (an `IsManagerOf`, an
  `IsWithinBudget`) to `TruthWeaver.Predicates` — the actual lookup
  semantics stay host-specific per ADR-0003, and tickets 01–06 document
  that as a per-host authoring pattern. Ticket 07 is the one narrow
  exception: a generic *factory* that wraps the resolve-then-test shape
  itself, which is general enough to ship precisely because it's agnostic
  to what's being resolved.
- Not implying every class-based predicate with an injected dependency is
  an instance of this pattern — `IsManager` and `HasEnoughRecentApprovals`
  (README's existing examples) use their injected dependency to read a
  value directly, not to *resolve a literal key into* a value; the
  distinguishing feature of this pattern family is specifically that a
  rule-text argument and/or context value is a **key to be resolved**, not
  a value already ready to use.
