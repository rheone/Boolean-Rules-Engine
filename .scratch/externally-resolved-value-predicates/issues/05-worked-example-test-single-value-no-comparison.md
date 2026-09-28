# 05: Worked-example test — single-value resolution, no comparison target

**What to build:** A test proving the pattern's third and most minimal
shape: the rule-text literal key resolves, through the injected service,
**directly to the boolean answer** — there is no second value to compare
against, and `TContext` may be entirely unused. This is the shape that
proves the pattern is not a "relationship" or even a two-value comparison
at all (e.g. a feature-flag-style predicate: `IsFeatureEnabled(flagKey: "new-checkout")`).

**Blocked by:** None (can start immediately; independent of tickets 01–04 and 06–07)

**Status:** done

Use `/tdd`: write the failing test first, then the minimal test-support
predicate/service to pass it.

- [x] Lives in the same test file as tickets 03/04 if that file already
  exists by the time this ticket is worked (no ordering dependency between
  03, 04, and 05 — they may land in any order), otherwise creates it.
- [x] Test-support predicate takes one literal key argument (any
  non-identity `LiteralKind`, e.g. `String`), does **not** read any value
  off `TContext` for comparison (a minimal or even empty context type is
  fine — the point is `TContext` doesn't need to participate at all), and
  an injected service (NSubstitute, per repo convention) resolves the key
  directly to a `bool`.
- [x] Test: when the injected service resolves the key to `true`, the
  predicate evaluates `true`; when `false`, the predicate evaluates
  `false` — i.e. the resolved value *is* the answer, with no additional
  comparison logic in the predicate itself.
- [x] Test or code comment makes explicit that this is deliberately the
  simplest instance of the same pattern family as tickets 03/04 (literal
  key + injected live resolution), distinguished only by having no second
  value and no context dependency — not a different pattern.
- [x] The injected lookup service is resolved fresh per evaluation call
  (per ADR-0002's scoped-resolution guarantee) — reuse or mirror the
  existing assertion style from
  `ScopedResolutionAndRegistrationTests.Class_based_predicate_is_resolved_fresh_from_the_service_provider_on_every_evaluation`
  rather than re-deriving it from scratch.

## Comments

Added to the existing `ExternallyResolvedRelationshipPredicateTests.cs`.
New test-support types: `TestSupport/IFeatureFlagService.cs`
(`ValueTask<bool> IsEnabledAsync(string flagKey, CancellationToken)`) and
`TestSupport/IsFeatureEnabled.cs` — a class-based
`IPredicate<RuleTestContext>` taking a `flagKey` `String` argument, whose
`EvaluateAsync` returns the injected service's resolved value directly
(no comparison logic at all), deliberately reusing the existing, already
empty `RuleTestContext` rather than introducing a new context type, since
it's never read. Three tests: flag resolved `true` → predicate `true`;
resolved `false` → predicate `false`; and the injected
`IFeatureFlagService`-backed predicate is resolved fresh from the service
provider on every evaluation, mirroring the fresh-resolution assertion
style used for tickets 03/04. A code comment above the first test states
this is deliberately the simplest instance of the same pattern family.
Reordered the test file so all `[Fact]` methods precede the private
helpers (SA1202) after adding this ticket's tests.
