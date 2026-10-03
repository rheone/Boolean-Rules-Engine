namespace TruthWeaver.Predicates;

using TruthWeaver.Abstractions;

/// <summary>
/// Adapts the built-in predicates' internally boolean comparisons to the Kleene
/// <see cref="TruthValue"/> a predicate delegate returns. Built-in predicates always reach a definite
/// answer (a null selected value is a definite <see cref="TruthValue.False"/>, never
/// <see cref="TruthValue.Unknown"/>), so only <c>True</c> and <c>False</c> are produced here.
/// </summary>
internal static class PredicateResult
{
    /// <summary>Wraps a definite boolean outcome as a completed predicate result.</summary>
    /// <param name="value">The boolean outcome of the comparison.</param>
    /// <returns>A completed <see cref="ValueTask{TruthValue}"/> holding <see cref="TruthValue.True"/> or <see cref="TruthValue.False"/>.</returns>
    public static ValueTask<TruthValue> FromBoolAsync(bool value)
    {
        return ValueTask.FromResult(value ? TruthValue.True : TruthValue.False);
    }
}
