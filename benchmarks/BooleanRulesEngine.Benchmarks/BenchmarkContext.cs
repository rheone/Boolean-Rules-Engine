namespace BooleanRulesEngine.Benchmarks;

/// <summary>
/// The application context type every benchmark rule evaluates against. Empty: every predicate in
/// this suite is a stateless, argument-driven lambda (<see cref="Testing.FakePredicates"/>) that
/// ignores the context entirely, so no benchmark-specific context data is needed. Public: BenchmarkDotNet
/// requires benchmark classes to be public, and this type appears in their public members' signatures
/// (e.g. <see cref="Compilation.CompilationResult{TContext}"/>).
/// </summary>
#pragma warning disable S2094 // Intentionally empty marker type - it never needs members, only identity as TContext.
public sealed class BenchmarkContext;
#pragma warning restore S2094
