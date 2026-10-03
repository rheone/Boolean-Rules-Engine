namespace TruthWeaver.Tests;

using NSubstitute;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// A predicate answers a Kleene <see cref="TruthValue"/> directly: <c>Unknown</c> is a normal answer
/// that records no <see cref="Fault"/>, while a throw is still the way to report a real failure
/// (ADR-0001, ADR-0005).
/// </summary>
public sealed class PredicateTruthValueTests
{
    /// <summary>A lambda predicate that returns Unknown evaluates to Unknown and records no fault.</summary>
    [Fact]
    public async Task EvaluateAsync_LambdaPredicateReturningUnknown_IsUnknownWithNoFault_Test()
    {
        // Arrange
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .Add(
                PredicateSchema.NoArguments("maybe", "maybe", "Always unknown."),
                (_, _, _) => ValueTask.FromResult(TruthValue.Unknown)
            );

        // Act
        Decision decision = await RunAsync("maybe", builder, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.Empty(decision.Faults);
    }

    /// <summary>A class-based predicate that returns Unknown evaluates to Unknown and records no fault.</summary>
    [Fact]
    public async Task EvaluateAsync_ClassPredicateReturningUnknown_IsUnknownWithNoFault_Test()
    {
        // Arrange
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .Add<UnknownPredicate>();
        CompiledRule<RuleTestContext> rule = new RuleCompiler<RuleTestContext>(builder.Build())
            .Compile("unknownClass")
            .CompiledRule!;
        IServiceProvider services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(UnknownPredicate)).Returns(new UnknownPredicate());

        // Act
        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            services,
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.Empty(decision.Faults);
    }

    /// <summary>An Unknown answer flows through the operators like any other Kleene value (Unknown OR True is True).</summary>
    [Fact]
    public async Task EvaluateAsync_UnknownPredicateOrTrue_IsTrue_Test()
    {
        // Arrange
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .Add(
                PredicateSchema.NoArguments("maybe", "maybe", "Always unknown."),
                (_, _, _) => ValueTask.FromResult(TruthValue.Unknown)
            )
            .AddConstant("yes", true);

        // Act
        Decision decision = await RunAsync("maybe OR yes", builder, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(TruthValue.True, decision.Result);
        Assert.Empty(decision.Faults);
    }

    /// <summary>A throwing predicate still evaluates to Unknown, and the throw is recorded as a fault.</summary>
    [Fact]
    public async Task EvaluateAsync_ThrowingPredicate_IsUnknownWithFault_Test()
    {
        // Arrange
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddThrowing("boom");

        // Act
        Decision decision = await RunAsync("boom", builder, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.Single(decision.Faults);
    }

    /// <summary>A predicate that cancels itself without the caller asking is Unknown plus a fault, not a cancellation.</summary>
    [Fact]
    public async Task EvaluateAsync_SelfCancelingPredicate_IsUnknownWithFault_Test()
    {
        // Arrange
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddSelfCancelingPredicate("selfCancels");

        // Act
        Decision decision = await RunAsync("selfCancels", builder, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.Single(decision.Faults);
    }

    /// <summary>A predicate that outlives the evaluation timeout surfaces the timeout as cancellation.</summary>
    [Fact]
    public Task EvaluateAsync_TimedOutPredicate_ThrowsCancellation_Test()
    {
        // Arrange
        CompiledRule<RuleTestContext> rule = new RuleCompiler<RuleTestContext>(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddDelayed("slow", TimeSpan.FromSeconds(5), true).Build()
        )
            .Compile("slow")
            .CompiledRule!;

        // Act / Assert
        return Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            rule.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                new EvaluationOptions(Timeout: TimeSpan.FromMilliseconds(50)),
                TestContext.Current.CancellationToken
            )
        );
    }

    /// <summary>A predicate that returns a definite value is unaffected: True and False pass through.</summary>
    [Theory]
    [InlineData(true, TruthValue.True)]
    [InlineData(false, TruthValue.False)]
    public async Task EvaluateAsync_DefinitePredicate_PassesValueThrough_Test(bool answer, TruthValue expected)
    {
        // Arrange
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("p", answer);

        // Act
        Decision decision = await RunAsync("p", builder, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expected, decision.Result);
        Assert.Empty(decision.Faults);
    }

    private static Task<Decision> RunAsync(
        string ruleText,
        PredicateRegistryBuilder<RuleTestContext> builder,
        CancellationToken cancellationToken
    )
    {
        CompiledRule<RuleTestContext> rule = new RuleCompiler<RuleTestContext>(builder.Build()).Compile(ruleText).CompiledRule!;
        return rule.EvaluateAsync(new RuleTestContext(), EmptyServiceProvider.Instance, cancellationToken: cancellationToken);
    }

    /// <summary>A class-based predicate that answers Unknown, used to exercise the service-provider path.</summary>
    private sealed class UnknownPredicate : IPredicate<RuleTestContext>
    {
        public static PredicateSchema Schema { get; } = PredicateSchema.NoArguments("unknownClass", "unknownClass", "Unknown.");

        public ValueTask<TruthValue> EvaluateAsync(
            RuleTestContext context,
            PredicateArguments args,
            CancellationToken cancellationToken
        )
        {
            return ValueTask.FromResult(TruthValue.Unknown);
        }
    }
}
