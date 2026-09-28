# 04: Worked-example test — non-identity value resolved through the same pattern

**What to build:** A test proving the pattern generalizes beyond identity
comparisons. Same shape as ticket 03 (context anchor + rule-text literal
key + injected live-lookup service), but the thing being compared is an
ordinary business value — e.g. a `Decimal` budget limit or threshold — not
a `Guid`/identity at all.

**Blocked by:** None (can start immediately; independent of tickets 01/02/03)

**Status:** done

Use `/tdd`: write the failing test first, then the minimal test-support
predicate/service to pass it.

- [x] Lives in the same test file as ticket 03 if that file already exists
  by the time this ticket is worked (check first — tickets 03 and 04 have
  no ordering dependency and may land in either order), otherwise creates
  it.
- [x] Example shape: a `String` cost-center-code argument (a key, not an
  identity) resolved via an injected `IBudgetLookupService` to a live
  `Decimal` spending limit, compared against a `Decimal` value read off
  `TContext` (e.g. a requested purchase amount) — or an equally valid
  non-identity example of the agent's choosing, as long as the argument's
  `LiteralKind` is not `Guid` and the resolved comparison value is not an
  identity/relationship, just an ordinary value.
- [x] Test: when the context-supplied amount is within the resolved limit,
  the predicate evaluates `true`.
- [x] Test: when it exceeds the resolved limit, the predicate evaluates
  `false`.
- [x] Test or code comment makes explicit that this is deliberately the
  same *shape* as ticket 03's identity case (context anchor + literal key +
  injected resolution + comparison), just with different `LiteralKind`s and
  a non-identity comparison — the point being proven is generality of the
  pattern, not a new pattern.

## Comments

Added to the existing `ExternallyResolvedRelationshipPredicateTests.cs`
(created by ticket 03, which landed first). New test-support types:
`TestSupport/PurchaseRequestContext.cs` (a `decimal Amount` context),
`TestSupport/IBudgetLookupService.cs`
(`ValueTask<decimal> ResolveLimitAsync(string costCenterCode, CancellationToken)`),
and `TestSupport/IsWithinBudget.cs` — a class-based
`IPredicate<PurchaseRequestContext>` taking a `costCenterCode` `String`
argument, resolving a live spending limit via the injected service and
comparing it against `context.Amount`. Two tests: amount within the
resolved limit → `true`; amount exceeding it → `false`. A code comment
immediately above the first test makes explicit this is deliberately the
same shape as ticket 03's `IsManagedByCandidate` case, just with a `String`
key and a non-identity `Decimal` comparison.
