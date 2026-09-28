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
}
