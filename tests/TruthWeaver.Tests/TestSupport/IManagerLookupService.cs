namespace TruthWeaver.Tests.TestSupport;

/// <summary>
/// A live-lookup dependency used to prove the externally-resolved-value predicate pattern's two-sided
/// shape (ticket 03): both a context anchor and a rule-text argument are resolved through this service
/// before comparison.
/// </summary>
public interface IManagerLookupService
{
    /// <summary>Resolves the resource's actual manager id, given the resource's id.</summary>
    /// <param name="resourceId">The resource to resolve a manager for.</param>
    /// <param name="cancellationToken">A token observed for cooperative cancellation.</param>
    /// <returns>The resolved manager id.</returns>
    public ValueTask<Guid> ResolveManagerIdAsync(Guid resourceId, CancellationToken cancellationToken);
}
