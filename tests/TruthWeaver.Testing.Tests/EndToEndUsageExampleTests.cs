namespace TruthWeaver.Testing.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;

/// <summary>
/// The worked example ticket 02 asks for: register a <see cref="FakePredicates"/> predicate against a
/// <see cref="PredicateRegistryBuilder{TContext}"/>, compile and evaluate a real rule against it, and
/// assert the resulting <see cref="Decision"/> using the <c>TruthWeaver.Testing</c> assertions
/// from ticket 01 - end to end, with no hand-written <see cref="IPredicate{TContext}"/> class.
/// </summary>
public sealed class EndToEndUsageExampleTests
{
    [Fact]
    public async Task FixedFakePredicate_DrivesASatisfiedDecision()
    {
        (PredicateSchema schema, Func<object?, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            FakePredicates.Returning<object?>("hasRole", true);

        PredicateRegistry<object?> registry = PredicateRegistry<object?>.CreateBuilder().Add(schema, evaluate).Build();
        RuleCompiler<object?> compiler = new(registry);
        CompiledRule<object?> rule = compiler.Compile("hasRole").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            null,
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        decision.Should().BeSatisfied().HaveResult(TruthValue.True).HaveNoFaults();
    }

    [Fact]
    public async Task FaultingFakePredicate_DrivesAnUnknownDecisionWithAFault()
    {
        (PredicateSchema schema, Func<object?, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            FakePredicates.Faulting<object?>("hasRole", new InvalidOperationException("simulated downstream failure"));

        PredicateRegistry<object?> registry = PredicateRegistry<object?>.CreateBuilder().Add(schema, evaluate).Build();
        RuleCompiler<object?> compiler = new(registry);
        CompiledRule<object?> rule = compiler.Compile("hasRole").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            null,
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        decision.Should().NotBeSatisfied().HaveResult(TruthValue.Unknown).HaveFaultForTerm("hasRole");
    }

    /// <summary>An <see cref="IServiceProvider"/> that resolves nothing - this example's rule uses only a lambda-registered fake predicate.</summary>
    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new();

        public object? GetService(Type serviceType)
        {
            return null;
        }
    }
}
