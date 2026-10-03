# 02: Predicates return TruthValue

**What to build:** Predicates can answer Unknown directly. IPredicate, all built-in predicate delegates, the externally-resolved-value helper delegate and FakePredicates return TruthValue; an Unknown result records no fault, while throw/timeout/cancellation still yield Unknown plus a Fault.

**Blocked by:** 01

**Status:** done

- [x] Predicate returning Unknown evaluates to Unknown with no Fault
- [x] Throwing, timed-out and cancelled predicates still give Unknown plus a Fault
- [x] FakePredicates returns Unknown directly and keeps a separate fault simulator
- [x] Built-in predicate libraries and tests migrated; null-selected-value convention preserved
- [x] Built test-first; the full validation set in CLAUDE.md passes (restore --locked-mode, build, test, csharpier check, format --verify-no-changes, roslynator analyze)

See [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

- `IPredicate<TContext>`, `PredicateDescriptor`/`PredicateRegistryBuilder.Add`, the evaluator, all built-in predicate delegates, `ResolvedValuePredicates` (including the `test` delegate) and `FakePredicates` now use `ValueTask<TruthValue>`. The evaluator no longer maps a bool; a returned `Unknown` records no `Fault`, while throw/timeout/cancellation behaviour is unchanged.
- `FakePredicates.Returning(TruthValue.Unknown)` and `Scripted` `Unknown` entries return `Unknown` directly; `Faulting` remains the fault simulator. A `Returning(bool)` overload is kept for convenience. `SimulatedPredicateFaultException` is kept (public API) but no longer thrown by the fakes; its docs now point at `Faulting`.
- Built-in predicates keep the null-selected-value convention (definite `False`, never `Unknown`) via an internal `PredicateResult.FromBoolAsync` helper.
- Tests: new `PredicateTruthValueTests` (lambda and class predicates answering Unknown, throw, self-cancel, timeout, definite pass-through), new Fake/ResolvedValue/end-to-end tests; the K3 oracle input helper now delivers Unknown directly instead of throwing. README, CONTEXT.md and ADR-0002 updated; the TODO in `IPredicate.cs` was removed.
