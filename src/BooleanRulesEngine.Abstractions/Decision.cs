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
/// <param name="EvaluatedTree">
/// A structural mirror of the compiled expression tree from this evaluation, with every node
/// (leaf or interior) annotated by what happened to it — its resolved result, or that it was skipped
/// by short-circuiting. Unlike <paramref name="Trace"/>'s flat log, this preserves the tree shape, so
/// it can drive a full-tree rendering (e.g. <c>MermaidTreePrinter</c>/<c>PlainTextTreePrinter</c>)
/// that shows the whole rule, the path actually taken, and the parts left out.
/// </param>
public sealed record Decision(
    TruthValue Result,
    IReadOnlyList<Fault> Faults,
    Trace? Trace = null,
    EvaluatedNode? EvaluatedTree = null
)
{
    /// <summary>
    /// Gets a value indicating whether this decision is satisfied. <see langword="true"/> only when
    /// <see cref="Result"/> is <see cref="TruthValue.True"/> — <see cref="TruthValue.Unknown"/> fails
    /// closed, the correct default for an authorization consumer (ADR-0001).
    /// </summary>
    public bool IsSatisfied => this.Result == TruthValue.True;
}
