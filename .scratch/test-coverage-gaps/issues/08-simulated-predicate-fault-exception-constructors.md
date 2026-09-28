# 08: Cover SimulatedPredicateFaultException constructors

**What to build:** Directly test each constructor of `SimulatedPredicateFaultException` (parameterless, message-only, message+innerException), which today is only exercised indirectly through the `ForPredicate(name)` factory.

**Blocked by:** None (can start immediately)

**Status:** done

- [ ] The parameterless constructor produces an exception with the expected default state.
- [ ] The message-only constructor sets `Message` as expected.
- [ ] The message+innerException constructor sets both `Message` and `InnerException` as expected.
- [ ] Existing `FakePredicates`/`ForPredicate` tests continue to pass unchanged.
