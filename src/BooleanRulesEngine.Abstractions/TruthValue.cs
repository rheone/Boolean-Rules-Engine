namespace BooleanRulesEngine.Abstractions;

/// <summary>
/// A three-valued (Kleene) truth value. Never represented as <c>bool?</c>:
/// a dedicated enum keeps <see cref="Unknown"/> out of accidental boolean contexts and gives
/// callers switch exhaustiveness. See ADR-0001 for the truth tables and the reasoning.
/// </summary>
public enum TruthValue
{
    /// <summary>The expression is definitely false.</summary>
    False,

    /// <summary>The expression is definitely true.</summary>
    True,

    /// <summary>
    /// The expression could not be determined — either a predicate faulted, or the value
    /// depends on an operand that itself is <see cref="Unknown"/> in a way the operator's
    /// truth table cannot resolve.
    /// </summary>
    Unknown,
}
