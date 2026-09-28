# 05: Worked-example test — external lookup failure is absorbed as a Fault, not an unhandled exception

**What to build:** A test proving that when the injected external-lookup
service used by this pattern throws (timeout, connection failure, etc.),
the evaluator absorbs it as a `Fault` and the term evaluates to
`TruthValue.Unknown`, per ADR-0001 and `CONTEXT.md#failure-model-summary` —
never an unhandled exception propagating out of `EvaluateAsync`. This is
the pattern's highest-risk edge (a live external call happening inside
predicate evaluation) and deserves explicit, dedicated coverage rather than
being assumed to fall out of whatever general fault-absorption tests exist
elsewhere in the suite.

**Blocked by:** None (can start immediately; independent of tickets 01–04)

**Status:** done

Use `/tdd`: write the test expecting `Fault`/`Unknown` behavior first,
against a deliberately-throwing fake lookup service, before touching
anything else.

- [x] Reuses the ticket 03 or 04 test-support predicate/service shape
  (whichever already exists at the time this is worked) rather than
  inventing a third variant, substituting a throwing implementation of the
  injected lookup service for the specific test.
- [x] Test: the injected lookup service throws (e.g. `TimeoutException` or
  another representative transient-failure exception) during
  `EvaluateAsync`; the resulting `Decision`/evaluated tree shows the term
  as `Unknown`, and a `Fault` recording the predicate identity and the
  thrown exception is present (match the existing fault-inspection API used
  elsewhere in the test suite — check `CompilationTests.cs`'s
  `Throwing_predicate_is_absorbed_as_a_fault_and_evaluation_completes` for
  the established assertion shape before writing a new one).
- [x] Test: evaluation completes (no unhandled exception escapes
  `EvaluateAsync`) and, where the surrounding rule logic permits it
  (e.g. `Unknown OR True`), the overall decision still reaches a
  determinate answer — mirroring the "a fault that can't affect the
  outcome shouldn't turn a transient blip into a denial" guarantee from
  `CONTEXT.md#failure-model-summary`.
- [x] Existing fault-absorption tests elsewhere in the suite continue to
  pass unchanged — this ticket adds coverage specific to the
  externally-resolved-relationship pattern, it does not replace or
  generalize the existing throwing-predicate tests.

## Comments

Added two tests to `ExternallyResolvedRelationshipPredicateTests.cs`,
reusing ticket 03's `IsManagedByCandidate`/`IManagerLookupService` shape via
a new `ServicesWithThrowingManagerLookup` helper that resolves an
`IManagerLookupService` substitute whose `ResolveManagerIdAsync` throws a
`TimeoutException`. First test asserts the resulting `Decision` is
`Unknown`, `IsSatisfied` is `false`, and a single `Fault` names
`"isManagedByCandidate"` and wraps the `TimeoutException`, matching
`CompilationTests.Throwing_predicate_is_absorbed_as_a_fault_and_evaluation_completes`'s
assertion shape. Second test compiles
`isManagedByCandidate(...) OR true` against the same throwing service and
asserts the overall decision is `True`/satisfied despite the fault — the
"a fault that can't affect the outcome shouldn't turn a transient blip into
a denial" guarantee. Existing `CompilationTests` fault-absorption tests were
left untouched and still pass. Used `.Returns<Guid>(_ => throw ...)` rather
than `NSubstitute.ExceptionExtensions.Throws` for the throwing setup, since
`Throws` on a `ValueTask<T>`-returning member triggered a Roslynator CA2012
info diagnostic under `dotnet format --verify-no-changes --severity info`;
the `Returns` overload with an explicit `<Guid>` type argument (needed to
disambiguate the `T`/`ValueTask<T>` overload pair) does not.
