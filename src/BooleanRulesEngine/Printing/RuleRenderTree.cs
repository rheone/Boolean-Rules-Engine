namespace BooleanRulesEngine.Printing;

using BooleanRulesEngine.Abstractions;
using BooleanRulesEngine.Evaluation;

/// <summary>
/// Zips a rule's <see cref="RuleDescription"/> (what to say — labels, always the full static shape)
/// against an optional <see cref="EvaluatedNode"/> (what happened — results and skips, only as deep as
/// evaluation actually went) into one <see cref="RenderNode"/> tree. Both source trees are built by
/// recursing over the same <c>Expression.Operands</c> lists in the same order — <c>CompiledRule.Describe()</c>
/// and <c>Evaluator</c> respectively — so they align positionally with no need to match by text or
/// replay any short-circuit logic here.
///
/// Every format-specific printer (<see cref="MermaidTreePrinter"/>, <see cref="PlainTextTreePrinter"/>)
/// renders this one shared tree, so "what a skipped subtree looks like" is decided once, not per format.
/// </summary>
internal static class RuleRenderTree
{
    /// <summary>Builds a purely structural render tree, with no evaluation coloring.</summary>
    /// <param name="description">The rule's described tree.</param>
    /// <returns>The render tree.</returns>
    public static RenderNode Build(RuleDescription description)
    {
        return Build(description, evaluated: null, ancestorSkipped: false);
    }

    /// <summary>Builds a render tree colored by one evaluation's result tree.</summary>
    /// <param name="description">The rule's described tree.</param>
    /// <param name="evaluated">The root of the matching <see cref="Decision.EvaluatedTree"/>.</param>
    /// <returns>The render tree.</returns>
    public static RenderNode Build(RuleDescription description, EvaluatedNode evaluated)
    {
        return Build(description, evaluated, ancestorSkipped: false);
    }

    private static RenderNode Build(RuleDescription description, EvaluatedNode? evaluated, bool ancestorSkipped)
    {
        bool skipped = ancestorSkipped || evaluated is { NotEvaluated: true };
        RenderState state = skipped ? RenderState.Skipped : StateFor(evaluated?.Result);

        // A skipped subtree's EvaluatedNode has no children (evaluation never recursed into it), so
        // there is nothing per-descendant to pass down — "skipped" itself propagates via `skipped`.
        IReadOnlyList<EvaluatedNode>? children = evaluated is { NotEvaluated: false } ? evaluated.Children : null;

        List<RenderNode> renderedChildren = new(description.Operands.Count);
        for (int i = 0; i < description.Operands.Count; i++)
        {
            EvaluatedNode? childEvaluated = children is { Count: > 0 } ? children[i] : null;
            renderedChildren.Add(Build(description.Operands[i], childEvaluated, skipped));
        }

        return new RenderNode(description.Label, state, renderedChildren);
    }

    private static RenderState StateFor(TruthValue? result)
    {
        return result switch
        {
            TruthValue.True => RenderState.True,
            TruthValue.False => RenderState.False,
            TruthValue.Unknown => RenderState.Indeterminate,
            null => RenderState.NoData,
            _ => throw new InvalidOperationException($"Unhandled truth value '{result}'."),
        };
    }
}
