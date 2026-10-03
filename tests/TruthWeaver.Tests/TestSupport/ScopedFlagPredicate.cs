namespace TruthWeaver.Tests.TestSupport;

using TruthWeaver.Abstractions;

/// <summary>A class-based predicate with a scoped dependency, registered by type against a <see cref="Registry.PredicateRegistry{TContext}"/>.</summary>
/// <remarks>Initializes a new instance of the <see cref="ScopedFlagPredicate"/> class.</remarks>
/// <param name="flag">The scoped dependency this predicate reads from.</param>
public sealed class ScopedFlagPredicate(IScopedFlag flag) : IPredicate<RuleTestContext>
{
    private readonly IScopedFlag flag = flag;

    /// <inheritdoc />
    public static PredicateSchema Schema =>
        PredicateSchema.NoArguments("scopedFlag", "Scoped Flag", "True iff the scoped IScopedFlag dependency's Value is set.");

    /// <inheritdoc />
    public ValueTask<TruthValue> EvaluateAsync(
        RuleTestContext context,
        PredicateArguments args,
        CancellationToken cancellationToken
    )
    {
        return ValueTask.FromResult(this.flag.Value ? TruthValue.True : TruthValue.False);
    }
}
