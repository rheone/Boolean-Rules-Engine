namespace TruthWeaver.Tests.TestSupport;

/// <summary>
/// A live-lookup dependency used to prove the externally-resolved-value predicate pattern's
/// single-sided shape (ticket 04): a rule-text <c>String</c> key (not an identity) resolves, through
/// this service, to a live <see cref="decimal"/> comparison value.
/// </summary>
public interface IBudgetLookupService
{
    /// <summary>Resolves the current spending limit for a cost center.</summary>
    /// <param name="costCenterCode">The cost center code to look up a live limit for.</param>
    /// <param name="cancellationToken">A token observed for cooperative cancellation.</param>
    /// <returns>The resolved spending limit.</returns>
    public ValueTask<decimal> ResolveLimitAsync(string costCenterCode, CancellationToken cancellationToken);
}
