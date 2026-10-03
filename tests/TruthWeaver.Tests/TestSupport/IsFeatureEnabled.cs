namespace TruthWeaver.Tests.TestSupport;

using TruthWeaver.Abstractions;

/// <summary>
/// A worked example of the externally-resolved-value predicate pattern's simplest shape (ticket 05):
/// the rule-text <c>flagKey</c> argument resolves, through <see cref="IFeatureFlagService"/>, directly
/// to the boolean answer — there is no second value to compare against, and <see cref="RuleTestContext"/>
/// (deliberately empty) is never read.
/// </summary>
/// <param name="flags">The injected live-lookup dependency, resolved fresh per evaluation (ADR-0002).</param>
public sealed class IsFeatureEnabled(IFeatureFlagService flags) : IPredicate<RuleTestContext>
{
    private readonly IFeatureFlagService flags = flags;

    /// <inheritdoc />
    public static PredicateSchema Schema =>
        new(
            "isFeatureEnabled",
            "Is Feature Enabled",
            "Is the given feature flag currently enabled, resolved live from the flag service?",
            [new PredicateArgumentSchema("flagKey", "The feature flag key to look up.", LiteralKind.String)]
        );

    /// <inheritdoc />
    public ValueTask<TruthValue> EvaluateAsync(
        RuleTestContext context,
        PredicateArguments args,
        CancellationToken cancellationToken
    )
    {
        return this.EvaluateCoreAsync(args.GetString("flagKey"), cancellationToken);
    }

    private async ValueTask<TruthValue> EvaluateCoreAsync(string flagKey, CancellationToken cancellationToken)
    {
        bool enabled = await this.flags.IsEnabledAsync(flagKey, cancellationToken);
        return enabled ? TruthValue.True : TruthValue.False;
    }
}
