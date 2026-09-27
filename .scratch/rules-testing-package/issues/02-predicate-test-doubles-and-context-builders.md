# 02: Predicate test doubles and context builders

**What to build:** Fake/stub predicate helpers in `BooleanRulesEngine.Testing`, so a consumer can register a predicate with a fixed or scripted answer (including a simulated fault) without writing a hand-written `IPredicate<TContext>` class per test.

**Blocked by:** 01 (BooleanRulesEngine.Testing package with fluent Decision assertions)

**Status:** done

- [x] A fake predicate helper lets a test register a named predicate returning a fixed `bool` (or Kleene value) without a hand-written class
- [x] A fake predicate helper supports simulating a fault (a thrown exception) for testing `Unknown`/fault-absorption behavior
- [x] Documentation/example shows registering a fake predicate against a `PredicateRegistryBuilder<TContext>` and asserting the resulting `Decision` using the assertions from ticket 01
