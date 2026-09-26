namespace BooleanRulesEngine.Abstractions;

/// <summary>
/// The result of evaluating a rule against a context: a <see cref="TruthValue"/> plus any faults
/// recorded along the way, and optionally a trace (ADR-0001).
/// </summary>
/// <param name="Result">The rule's three-valued result.</param>
/// <param name="Faults">Every fault absorbed during this evaluation, in the order they occurred.</param>
/// <param name="Trace">
/// The evaluation trace, present only when requested via <c>EvaluationOptions</c>.
/// </param>
public sealed record Decision(TruthValue Result, IReadOnlyList<Fault> Faults, Trace? Trace = null)
{
    /// <summary>
    /// Gets a value indicating whether this decision is satisfied. <see langword="true"/> only when
    /// <see cref="Result"/> is <see cref="TruthValue.True"/> — <see cref="TruthValue.Unknown"/> fails
    /// closed, the correct default for an authorization consumer (ADR-0001).
    /// </summary>
    public bool IsSatisfied => this.Result == TruthValue.True;
}
