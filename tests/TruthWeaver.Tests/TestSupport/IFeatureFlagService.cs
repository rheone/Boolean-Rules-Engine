namespace TruthWeaver.Tests.TestSupport;

/// <summary>
/// A live-lookup dependency used to prove the externally-resolved-value predicate pattern's simplest
/// shape (ticket 05): the literal key resolves directly to the boolean answer — there is no second
/// value to compare against.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>Resolves whether a feature flag is currently enabled.</summary>
    /// <param name="flagKey">The feature flag key to look up.</param>
    /// <param name="cancellationToken">A token observed for cooperative cancellation.</param>
    /// <returns><see langword="true"/> if the flag is enabled.</returns>
    public ValueTask<bool> IsEnabledAsync(string flagKey, CancellationToken cancellationToken);
}
