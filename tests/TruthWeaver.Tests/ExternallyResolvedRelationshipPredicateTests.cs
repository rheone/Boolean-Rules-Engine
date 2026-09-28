namespace TruthWeaver.Tests;

using NSubstitute;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Externally-resolved-value predicates tickets 03-06: worked-example tests for the pattern where a
/// rule-text literal argument and/or a <c>TContext</c>-supplied value is a key resolved live through a
/// constructor-injected service, rather than a value already ready to use.
/// </summary>
/// <remarks>
/// Term identity for this pattern's literal argument is an ordinary case of the general term-identity
/// rule already covered end to end by <see cref="ArgumentsAndMemoizationTests"/> (same predicate name +
/// same literal argument value = same term, regardless of what a class-based predicate's injected
/// dependency happens to resolve it to at evaluation time) — nothing about external resolution changes
/// that rule, so it is not re-asserted here.
/// </remarks>
public sealed class ExternallyResolvedRelationshipPredicateTests
{
    private static readonly Guid ResourceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ActualManagerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid DifferentManagerId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public async Task Candidate_resolved_as_the_resources_actual_manager_evaluates_true()
    {
        IServiceProvider services = ServicesResolvingManagerAs(ActualManagerId);
        CompiledRule<ResourceContext> rule = CompileIsManagedByCandidate();

        Decision decision = await rule.EvaluateAsync(
            new ResourceContext(ResourceId),
            services,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    [Fact]
    public async Task Candidate_resolved_as_a_different_manager_evaluates_false()
    {
        IServiceProvider services = ServicesResolvingManagerAs(DifferentManagerId);
        CompiledRule<ResourceContext> rule = CompileIsManagedByCandidate();

        Decision decision = await rule.EvaluateAsync(
            new ResourceContext(ResourceId),
            services,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.False, decision.Result);
    }

    [Fact]
    public async Task Injected_lookup_service_is_resolved_fresh_from_the_service_provider_on_every_evaluation()
    {
        List<IsManagedByCandidate> resolvedInstances = [];
        IServiceProvider services = Substitute.For<IServiceProvider>();
        services
            .GetService(typeof(IsManagedByCandidate))
            .Returns(_ =>
            {
                IManagerLookupService lookup = Substitute.For<IManagerLookupService>();
                lookup.ResolveManagerIdAsync(ResourceId, Arg.Any<CancellationToken>()).Returns(ActualManagerId);
                IsManagedByCandidate instance = new(lookup);
                resolvedInstances.Add(instance);
                return instance;
            });

        CompiledRule<ResourceContext> rule = CompileIsManagedByCandidate();

        await rule.EvaluateAsync(
            new ResourceContext(ResourceId),
            services,
            cancellationToken: TestContext.Current.CancellationToken
        );
        await rule.EvaluateAsync(
            new ResourceContext(ResourceId),
            services,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(2, resolvedInstances.Count);
        Assert.NotSame(resolvedInstances[0], resolvedInstances[1]);
    }

    // Deliberately the same shape as the two-sided IsManagedByCandidate tests above (context anchor +
    // literal key + injected live resolution + comparison) — just with a String key rather than a
    // Guid, and a non-identity Decimal comparison value read straight off the context rather than a
    // second resolved side. The point is the pattern's generality, not a different pattern.
    [Fact]
    public async Task Context_amount_within_the_resolved_limit_evaluates_true()
    {
        IServiceProvider services = ServicesResolvingLimitAs(500m);
        CompiledRule<PurchaseRequestContext> rule = CompileIsWithinBudget();

        Decision decision = await rule.EvaluateAsync(
            new PurchaseRequestContext(Amount: 500m),
            services,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    [Fact]
    public async Task Context_amount_exceeding_the_resolved_limit_evaluates_false()
    {
        IServiceProvider services = ServicesResolvingLimitAs(500m);
        CompiledRule<PurchaseRequestContext> rule = CompileIsWithinBudget();

        Decision decision = await rule.EvaluateAsync(
            new PurchaseRequestContext(Amount: 500.01m),
            services,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.False, decision.Result);
    }

    // The simplest instance of the same pattern family as the two tests above: a literal key +
    // injected live resolution, but with no second value and no context dependency at all — the
    // resolved value *is* the answer, not a different pattern.
    [Fact]
    public async Task Flag_resolved_true_evaluates_true()
    {
        IServiceProvider services = ServicesResolvingFlagAs(true);
        CompiledRule<RuleTestContext> rule = CompileIsFeatureEnabled();

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            services,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    [Fact]
    public async Task Flag_resolved_false_evaluates_false()
    {
        IServiceProvider services = ServicesResolvingFlagAs(false);
        CompiledRule<RuleTestContext> rule = CompileIsFeatureEnabled();

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            services,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.False, decision.Result);
    }

    [Fact]
    public async Task Feature_flag_lookup_service_is_resolved_fresh_from_the_service_provider_on_every_evaluation()
    {
        List<IsFeatureEnabled> resolvedInstances = [];
        IServiceProvider services = Substitute.For<IServiceProvider>();
        services
            .GetService(typeof(IsFeatureEnabled))
            .Returns(_ =>
            {
                IFeatureFlagService flags = Substitute.For<IFeatureFlagService>();
                flags.IsEnabledAsync("new-checkout", Arg.Any<CancellationToken>()).Returns(true);
                IsFeatureEnabled instance = new(flags);
                resolvedInstances.Add(instance);
                return instance;
            });

        CompiledRule<RuleTestContext> rule = CompileIsFeatureEnabled();

        await rule.EvaluateAsync(new RuleTestContext(), services, cancellationToken: TestContext.Current.CancellationToken);
        await rule.EvaluateAsync(new RuleTestContext(), services, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, resolvedInstances.Count);
        Assert.NotSame(resolvedInstances[0], resolvedInstances[1]);
    }

    private static IServiceProvider ServicesResolvingManagerAs(Guid resolvedManagerId)
    {
        IManagerLookupService lookup = Substitute.For<IManagerLookupService>();
        lookup.ResolveManagerIdAsync(ResourceId, Arg.Any<CancellationToken>()).Returns(resolvedManagerId);

        IServiceProvider services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IsManagedByCandidate)).Returns(new IsManagedByCandidate(lookup));
        return services;
    }

    private static CompiledRule<ResourceContext> CompileIsManagedByCandidate()
    {
        RuleCompiler<ResourceContext> compiler = new(
            PredicateRegistry<ResourceContext>.CreateBuilder().Add<IsManagedByCandidate>().Build()
        );
        return compiler
            .Compile("isManagedByCandidate(candidateManagerId: \"22222222-2222-2222-2222-222222222222\")")
            .CompiledRule!;
    }

    private static IServiceProvider ServicesResolvingLimitAs(decimal resolvedLimit)
    {
        IBudgetLookupService lookup = Substitute.For<IBudgetLookupService>();
        lookup.ResolveLimitAsync("CC-100", Arg.Any<CancellationToken>()).Returns(resolvedLimit);

        IServiceProvider services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IsWithinBudget)).Returns(new IsWithinBudget(lookup));
        return services;
    }

    private static CompiledRule<PurchaseRequestContext> CompileIsWithinBudget()
    {
        RuleCompiler<PurchaseRequestContext> compiler = new(
            PredicateRegistry<PurchaseRequestContext>.CreateBuilder().Add<IsWithinBudget>().Build()
        );
        return compiler.Compile("isWithinBudget(costCenterCode: \"CC-100\")").CompiledRule!;
    }

    private static IServiceProvider ServicesResolvingFlagAs(bool resolvedValue)
    {
        IFeatureFlagService flags = Substitute.For<IFeatureFlagService>();
        flags.IsEnabledAsync("new-checkout", Arg.Any<CancellationToken>()).Returns(resolvedValue);

        IServiceProvider services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IsFeatureEnabled)).Returns(new IsFeatureEnabled(flags));
        return services;
    }

    private static CompiledRule<RuleTestContext> CompileIsFeatureEnabled()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Add<IsFeatureEnabled>().Build()
        );
        return compiler.Compile("isFeatureEnabled(flagKey: \"new-checkout\")").CompiledRule!;
    }
}
