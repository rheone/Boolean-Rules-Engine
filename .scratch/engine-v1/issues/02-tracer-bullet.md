# 02: Tracer bullet: compile → evaluate a bare rule

**What to build:** The full pipeline, end to end, for the smallest possible rule shape: a single zero-argument predicate reference, or a `true`/`false` constant. This is the tracer bullet every later ticket extends — nothing about operators, arguments, or serialization formats beyond this minimal case is in scope here.

A developer can: implement `IPredicate<TContext>` with a static `PredicateSchema` and an async `EvaluateAsync`; register it (a lambda-based registration is enough for this ticket) against a `PredicateRegistry`; write rule text that is just a predicate name (e.g. `isManager`) or a constant (`true`/`false`); call `RuleCompiler.Compile` and get back a `CompilationResult` (nullable `CompiledRule` + diagnostics) that never throws, with an unknown predicate name producing an `Error`-severity diagnostic with a source span instead of an exception; and call `CompiledRule.EvaluateAsync` against a context and `IServiceProvider` to get a `Decision` (`TruthValue Result`, `IReadOnlyList<Fault> Faults`, `bool IsSatisfied` — `true` only when `Result == TruthValue.True`).

A predicate that throws during evaluation is caught at the term boundary, recorded as a `Fault` (term identity + exception), and the term's value is `TruthValue.Unknown` rather than the exception propagating out of `EvaluateAsync`.

**Blocked by:** 01

**Status:** done (verified against existing codebase — already implemented prior to this pass)

- [ ] `TruthValue` (`False`/`True`/`Unknown`), `Decision`, and `Fault` exist as described in ADR-0001; `bool?` does not appear anywhere on the public surface
- [ ] `IPredicate<TContext>` and `PredicateSchema` exist per ADR-0002's shape (`static abstract PredicateSchema Schema`, `ValueTask<bool> EvaluateAsync(TContext, PredicateArguments, CancellationToken)` — the arguments accessor itself can be a stub for this ticket since no rule here has arguments)
- [ ] A rule that is just a registered zero-arg predicate name compiles successfully and evaluates to `True`/`False` matching what the predicate returns
- [ ] A rule referencing an unregistered predicate name produces a `CompilationResult` with a null `CompiledRule` and at least one `Error` diagnostic with a code and source span; `Compile` does not throw
- [ ] A rule that is the literal `true` or `false` compiles and evaluates to the matching constant
- [ ] A predicate that throws during `EvaluateAsync` results in a `Decision` with `Result == Unknown`, `IsSatisfied == false`, and a `Fault` describing the term and the exception — evaluation completes rather than the exception propagating
- [ ] `Decision.IsSatisfied` is `true` if and only if `Result == TruthValue.True`
