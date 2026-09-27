# 12: Scoped DI predicate resolution & registration extensions

**What to build:** Class-based `IPredicate<TContext>` implementations, registered against an explicit `PredicateRegistry` builder API (alongside the lambda-based registration already in place from ticket 02), resolved from a per-evaluation `IServiceProvider` supplied alongside the context — not captured once at registration time. This matters specifically for predicates with scoped dependencies (a `DbContext`, a per-request `HttpClient`): a predicate registered once but resolved fresh per evaluation gets a correctly-scoped instance every time, rather than a stale one captured at registration and reused incorrectly across scopes.

Also add DI registration extensions (`IServiceCollection` extension methods) for wiring a `PredicateRegistry` and `RuleCompiler` into a host application's container — the "no attribute/assembly scanning, explicit registration only" model from ADR-0002/ADR-0004 applies here too.

**Blocked by:** 02

**Status:** done (verified against existing codebase — already implemented prior to this pass)

- [ ] A class-based `IPredicate<TContext>` can be registered against `PredicateRegistry` by type (not just by lambda), and a compiled rule referencing it evaluates correctly
- [ ] Using a substitute `IServiceProvider` (NSubstitute) that returns a distinct scoped service instance per call, two `EvaluateAsync` calls on the same `CompiledRule` each resolve a fresh predicate instance from the `IServiceProvider` passed to that call — proving the predicate is not captured once at registration time
- [ ] There is no attribute-scanning or assembly-scanning registration path — registration is exclusively through the explicit builder API and the lambda form
- [ ] `IServiceCollection` extension method(s) exist to register a `PredicateRegistry` (and whatever else is needed to resolve a `RuleCompiler`) into a host application's DI container, and a minimal integration test proves a rule can be compiled and evaluated using only container-resolved services
