namespace TruthWeaver.Tests;

using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.DependencyInjection;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>Ticket 12: scoped DI predicate resolution and DI registration extensions.</summary>
public sealed class ScopedResolutionAndRegistrationTests
{
    [Fact]
    public async Task Class_based_predicate_registered_by_type_evaluates_correctly()
    {
        IScopedFlag flag = Substitute.For<IScopedFlag>();
        flag.Value.Returns(true);
        IServiceProvider services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(ScopedFlagPredicate)).Returns(new ScopedFlagPredicate(flag));

        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Add<ScopedFlagPredicate>().Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("scopedFlag").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            services,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    [Fact]
    public async Task Class_based_predicate_is_resolved_fresh_from_the_service_provider_on_every_evaluation()
    {
        List<ScopedFlagPredicate> resolvedInstances = [];
        IServiceProvider services = Substitute.For<IServiceProvider>();
        services
            .GetService(typeof(ScopedFlagPredicate))
            .Returns(_ =>
            {
                IScopedFlag flag = Substitute.For<IScopedFlag>();
                flag.Value.Returns(true);
                ScopedFlagPredicate instance = new(flag);
                resolvedInstances.Add(instance);
                return instance;
            });

        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Add<ScopedFlagPredicate>().Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("scopedFlag").CompiledRule!;

        await rule.EvaluateAsync(new RuleTestContext(), services, cancellationToken: TestContext.Current.CancellationToken);
        await rule.EvaluateAsync(new RuleTestContext(), services, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, resolvedInstances.Count);
        Assert.NotSame(resolvedInstances[0], resolvedInstances[1]);
    }

    [Fact]
    public async Task Class_based_predicate_with_no_service_registration_is_absorbed_as_a_fault_naming_the_unresolved_type()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Add<ScopedFlagPredicate>().Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("scopedFlag").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            TestSupport.EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Fault fault = Assert.Single(decision.Faults);
        Assert.Equal("scopedFlag", fault.Term.PredicateName);
        InvalidOperationException exception = Assert.IsType<InvalidOperationException>(fault.Exception);
        Assert.Contains(nameof(ScopedFlagPredicate), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Service_collection_extension_wires_a_compiler_and_rule_using_only_container_resolved_services()
    {
        ServiceCollection services = new();
        services.AddScoped<IScopedFlag>(_ => new StubScopedFlag(true));

        // The predicate registry only records ScopedFlagPredicate's type; the host application's
        // container must separately register that concrete type as a resolvable service, exactly as
        // it would for any other class-based dependency (ADR-0002).
        services.AddScoped<ScopedFlagPredicate>();
        services.AddTruthWeaver<RuleTestContext>(builder => builder.Add<ScopedFlagPredicate>());

        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        RuleCompiler<RuleTestContext> compiler = scope.ServiceProvider.GetRequiredService<RuleCompiler<RuleTestContext>>();
        CompiledRule<RuleTestContext> rule = compiler.Compile("scopedFlag").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            scope.ServiceProvider,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    private sealed class StubScopedFlag(bool value) : IScopedFlag
    {
        public bool Value { get; } = value;
    }
}
