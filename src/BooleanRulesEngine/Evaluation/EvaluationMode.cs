namespace BooleanRulesEngine.Evaluation;

/// <summary>How thoroughly an evaluation visits the tree (ADR-0002).</summary>
public enum EvaluationMode
{
    /// <summary>Left-to-right with short-circuiting: <c>AND</c> stops at the first <c>False</c>, <c>OR</c> at the first <c>True</c>.</summary>
    Default,

    /// <summary>
    /// Evaluates every reachable term (no short-circuit) and collects every fault, for
    /// diagnostic/support use. Never changes <see cref="Abstractions.Decision.Result"/> — only which
    /// terms run and what the trace/fault list contains.
    /// </summary>
    Exhaustive,
}
