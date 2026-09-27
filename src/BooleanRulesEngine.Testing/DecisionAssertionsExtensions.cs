namespace BooleanRulesEngine.Testing;

using BooleanRulesEngine.Abstractions;

/// <summary>Entry point for fluent assertions over a <see cref="Decision"/>.</summary>
public static class DecisionAssertionsExtensions
{
    /// <summary>Starts a fluent assertion chain over <paramref name="decision"/>.</summary>
    /// <param name="decision">The decision under test.</param>
    /// <returns>A <see cref="DecisionAssertions"/> wrapping <paramref name="decision"/>.</returns>
    public static DecisionAssertions Should(this Decision decision)
    {
        return new DecisionAssertions(decision);
    }
}
