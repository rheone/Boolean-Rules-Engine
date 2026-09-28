# 03: Worked-example test — identity-shaped relationship, both sides externally resolved

**What to build:** A test proving the pattern where **both** the context
anchor and the rule-text argument require external resolution before
comparison — not a shortcut case where `TContext` already holds the exact
value being compared. This directly tests the corrected framing from
`spec.md`: neither side is privileged, and `TContext` is not assumed to be
"the current user."

**Blocked by:** None (can start immediately; independent of tickets 01/02)

**Status:** done

Use `/tdd` for this ticket: write the test against the class-based
predicate shape first, expecting it to fail for the right reason (no
predicate/service exists yet) before implementing the minimal
test-support predicate and fake/substitute lookup service needed to make
it pass.

- [x] New test file `tests/TruthWeaver.Tests/ExternallyResolvedRelationshipPredicateTests.cs`
  (or an existing suitable file if one already fits this shape — check
  `ScopedResolutionAndRegistrationTests.cs` first, since it already covers
  class-based DI resolution and might be the more consistent home; the
  agent decides based on what's actually there, not this ticket's guess).
- [x] Test-support predicate: a context type that does **not** represent
  "the current user" (e.g. a `Resource` with only a `ResourceId`, no user
  field), a candidate-party `Guid` rule-text argument, and an injected
  lookup service (NSubstitute, per repo convention) that resolves **both**
  the resource's actual manager (from the context anchor) and validates the
  candidate (from the argument) — mirroring ticket 01's `IsManagedByCandidate`
  example if that ticket has already landed, or the agent's own equivalent
  if it hasn't (tickets 01 and 03 have no dependency on each other and may
  land in either order).
- [x] Test: when the injected service resolves the candidate as the
  resource's actual manager, the predicate evaluates `true`.
- [x] Test: when it resolves to a different manager, the predicate
  evaluates `false`.
- [x] Test: the injected lookup service is resolved fresh per evaluation
  call (per ADR-0002's scoped-resolution guarantee) — reuse or mirror the
  existing assertion style from
  `ScopedResolutionAndRegistrationTests.Class_based_predicate_is_resolved_fresh_from_the_service_provider_on_every_evaluation`
  rather than re-deriving it from scratch.
- [x] Term identity is unaffected: two terms with the same literal
  `candidateManagerId` argument are the same term regardless of what the
  service resolves it to on a given evaluation (assert via the existing
  memoization/identity test patterns in `ArgumentsAndMemoizationTests.cs`
  if a natural assertion point exists, otherwise a direct comment noting
  why it's out of scope for this specific test file).

## Comments

Created `tests/TruthWeaver.Tests/ExternallyResolvedRelationshipPredicateTests.cs`
(ticket 01 had not landed a same-named example at the time this was worked,
so this ticket introduces the shared test-support types). Added
`TestSupport/ResourceContext.cs` (a `Guid ResourceId`-only context, no user
field), `TestSupport/IManagerLookupService.cs`
(`ValueTask<Guid> ResolveManagerIdAsync(Guid resourceId, CancellationToken)`),
and `TestSupport/IsManagedByCandidate.cs` — a class-based
`IPredicate<ResourceContext>` taking a `candidateManagerId` `Guid` argument,
resolving the resource's actual manager via the injected service and
comparing. Three tests exercise the new predicate/service pair: candidate
resolved as the actual manager → `true`; resolved as a different manager →
`false`; and the injected `IManagerLookupService`-backed predicate is
resolved fresh from the service provider on every `EvaluateAsync` call
(mirroring `ScopedResolutionAndRegistrationTests`'s fresh-resolution
assertion style — two resolved instances, not the same reference). Term
identity is left to a `<remarks>` comment on the test class explaining why
it's out of scope here (already covered generally by
`ArgumentsAndMemoizationTests`, and nothing about external resolution
changes that rule). Fixed line-ending (`ENDOFLINE`) and one missing
accessibility-modifier warning via `dotnet format` before final
`csharpier`/`format` verification.
