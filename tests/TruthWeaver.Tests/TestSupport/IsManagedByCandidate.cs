namespace TruthWeaver.Tests.TestSupport;

using TruthWeaver.Abstractions;

/// <summary>
/// A worked example of the externally-resolved-value predicate pattern's two-sided shape (ticket 03):
/// both the context anchor (<see cref="ResourceContext.ResourceId"/>) and the rule-text
/// <c>candidateManagerId</c> argument are resolved through <see cref="IManagerLookupService"/> before
/// comparison. Neither side is "the current user" — the context is a resource id, not a party.
/// </summary>
/// <param name="managers">The injected live-lookup dependency, resolved fresh per evaluation (ADR-0002).</param>
public sealed class IsManagedByCandidate(IManagerLookupService managers) : IPredicate<ResourceContext>
{
    private readonly IManagerLookupService managers = managers;

    /// <inheritdoc />
    public static PredicateSchema Schema =>
        new(
            "isManagedByCandidate",
            "Is Managed By Candidate",
            "Does the resource's actual manager, resolved live, match the given candidate?",
            [new PredicateArgumentSchema("candidateManagerId", "The candidate manager to validate.", LiteralKind.Guid)]
        );

    /// <inheritdoc />
    public async ValueTask<TruthValue> EvaluateAsync(
        ResourceContext context,
        PredicateArguments args,
        CancellationToken cancellationToken
    )
    {
        Guid candidateManagerId = args.GetGuid("candidateManagerId");
        Guid actualManagerId = await this.managers.ResolveManagerIdAsync(context.ResourceId, cancellationToken);
        return actualManagerId == candidateManagerId ? TruthValue.True : TruthValue.False;
    }
}
