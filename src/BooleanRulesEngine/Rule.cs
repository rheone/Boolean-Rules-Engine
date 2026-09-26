namespace BooleanRulesEngine;

/// <summary>
/// Represents a boolean rule that can be evaluated.
/// </summary>
public abstract class Rule
{
    /// <summary>
    /// Evaluates the rule.
    /// </summary>
    /// <returns>The result of evaluating this rule.</returns>
    public abstract bool Evaluate();
}
