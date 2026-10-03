namespace TruthWeaver.Abstractions;

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
/// <param name="Outcome">
/// The collapsed answer, present only when the rule declared an outermost <c>Collapse(expr, policy)</c>
/// (ADR-0005 decision 14); <see langword="null"/> for a rule that did not, so a caller that never declares a collapse sees
/// no change. When the declared policy is <see cref="CollapsePolicy.UnknownAsFalse"/> or
/// <see cref="CollapsePolicy.UnknownAsTrue"/> <see cref="Result"/> is already the collapsed, definite value; for
/// <see cref="CollapsePolicy.UnknownIsError"/> it stays the three-valued result and this is
/// <see cref="CollapseOutcome.RejectedUnresolved"/> when that result is <see cref="TruthValue.Unknown"/>. The inner,
/// uncollapsed result is always available as the single child of <see cref="EvaluatedTree"/>. For a collapse chosen at the
/// call site instead, use <see cref="Collapse(CollapsePolicy)"/>.
/// </param>
public sealed record Decision(
    TruthValue Result,
    IReadOnlyList<Fault> Faults,
    Trace? Trace = null,
    EvaluatedNode? EvaluatedTree = null,
    CollapseOutcome? Outcome = null
)
{
    /// <summary>
    /// Gets a value indicating whether this decision is satisfied. <see langword="true"/> only when
    /// <see cref="Result"/> is <see cref="TruthValue.True"/> — <see cref="TruthValue.Unknown"/> fails
    /// closed, the correct default for an authorization consumer (ADR-0001).
    /// </summary>
    public bool IsSatisfied => this.Result == TruthValue.True;

    /// <summary>
    /// Collapses <see cref="Result"/> to a final answer under <paramref name="policy"/>. <see cref="TruthValue.True"/> and
    /// <see cref="TruthValue.False"/> always map to the matching <see cref="CollapseOutcome"/>; only
    /// <see cref="TruthValue.Unknown"/> depends on the policy. Pure: it does not change this decision, its
    /// <see cref="Faults"/> or <see cref="IsSatisfied"/> (which stays fail-closed regardless of any policy applied here).
    /// </summary>
    /// <param name="policy">How an <see cref="TruthValue.Unknown"/> result is resolved.</param>
    /// <returns>The collapsed outcome.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="policy"/> is not a defined <see cref="CollapsePolicy"/>.</exception>
    public CollapseOutcome Collapse(CollapsePolicy policy)
    {
        return this.Result switch
        {
            TruthValue.True => CollapseOutcome.True,
            TruthValue.False => CollapseOutcome.False,
            _ => policy switch
            {
                CollapsePolicy.UnknownAsFalse => CollapseOutcome.False,
                CollapsePolicy.UnknownAsTrue => CollapseOutcome.True,
                CollapsePolicy.UnknownIsError => CollapseOutcome.RejectedUnresolved,
                _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unhandled collapse policy."),
            },
        };
    }
}
