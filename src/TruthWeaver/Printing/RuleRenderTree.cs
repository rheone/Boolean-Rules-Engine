namespace TruthWeaver.Printing;

using TruthWeaver.Abstractions;
using TruthWeaver.Evaluation;

/// <summary>
/// <para>
/// Zips a rule's <see cref="RuleDescription"/> (what to say — labels, always the full static shape)
/// against an optional <see cref="EvaluatedNode"/> (what happened — results and skips, only as deep as
/// evaluation actually went) into one <see cref="RenderNode"/> tree. Both source trees are built from
/// the same shared node-shape seam (<c>ExpressionShape.Of</c>) — <c>CompiledRule.DescribeNode</c> and
/// <c>Evaluator</c> respectively — so they align positionally with no need to match by text or replay
/// any short-circuit logic here.
/// </para>
/// <para>
/// Every format-specific printer (<see cref="MermaidTreePrinter"/>, <see cref="PlainTextTreePrinter"/>)
/// renders this one shared tree, so "what a skipped subtree looks like" is decided once, not per format.
/// </para>
/// </summary>
internal static class RuleRenderTree
{
    /// <summary>Builds a purely structural render tree, with no evaluation coloring.</summary>
    /// <param name="description">The rule's described tree.</param>
    /// <param name="style">How to render the AND/OR/NOT/XOR/EQUIVALENT operator labels.</param>
    /// <param name="showArgumentValues">Whether to include a term's <see cref="RuleDescription.ArgumentText"/> in its rendered label.</param>
    /// <returns>The render tree.</returns>
    public static RenderNode Build(
        RuleDescription description,
        OperatorStyle style = OperatorStyle.Word,
        bool showArgumentValues = true
    )
    {
        return Build(description, evaluated: null, ancestorSkipped: false, style, showArgumentValues);
    }

    /// <summary>Builds a render tree colored by one evaluation's result tree.</summary>
    /// <param name="description">The rule's described tree.</param>
    /// <param name="evaluated">The root of the matching <see cref="Decision.EvaluatedTree"/>.</param>
    /// <param name="style">How to render the AND/OR/NOT/XOR/EQUIVALENT operator labels.</param>
    /// <param name="showArgumentValues">Whether to include a term's <see cref="RuleDescription.ArgumentText"/> in its rendered label.</param>
    /// <returns>The render tree.</returns>
    public static RenderNode Build(
        RuleDescription description,
        EvaluatedNode evaluated,
        OperatorStyle style = OperatorStyle.Word,
        bool showArgumentValues = true
    )
    {
        return Build(description, evaluated, ancestorSkipped: false, style, showArgumentValues);
    }

    private static RenderNode Build(
        RuleDescription description,
        EvaluatedNode? evaluated,
        bool ancestorSkipped,
        OperatorStyle style,
        bool showArgumentValues
    )
    {
        AssertOperandCountsAligned(description, evaluated);

        bool skipped = ancestorSkipped || evaluated is { NotEvaluated: true };
        RenderState state = skipped ? RenderState.Skipped : StateFor(evaluated?.Result);

        // A skipped subtree's EvaluatedNode has no children (evaluation never recursed into it), so
        // there is nothing per-descendant to pass down — "skipped" itself propagates via `skipped`.
        IReadOnlyList<EvaluatedNode>? children = evaluated is { NotEvaluated: false } ? evaluated.Children : null;

        List<RenderNode> renderedChildren = new(description.Operands.Count);
        for (int i = 0; i < description.Operands.Count; i++)
        {
            EvaluatedNode? childEvaluated = children is { Count: > 0 } ? children[i] : null;
            renderedChildren.Add(Build(description.Operands[i], childEvaluated, skipped, style, showArgumentValues));
        }

        return new RenderNode(StyledLabel(description, style, showArgumentValues), state, renderedChildren);
    }

    /// <summary>
    /// Renders <paramref name="description"/>'s label in <paramref name="style"/>, with its
    /// <see cref="RuleDescription.ArgumentText"/> appended when <paramref name="showArgumentValues"/>
    /// is <see langword="true"/> and the term has any. Only an operator node's exact word-form label
    /// (<c>AND</c>, <c>OR</c>, <c>NOT</c>, <c>XOR</c>, <c>EQUIVALENT</c>, <c>IMPLIES</c>, <c>NAND</c>, <c>NOR</c>) with at least one operand is
    /// eligible for restyling — a term or constant leaf (always zero operands) is never restyled even
    /// if a predicate's authored label happens to collide with one of those words, and
    /// <c>ExactlyOne</c>/threshold/<c>BETWEEN</c>/<c>If</c>/inspection labels (e.g. <c>AtLeast(3)</c>) fall through unchanged in every
    /// style, since they have no symbolic or C-style spelling. <c>COALESCE</c> renders as <c>??</c> in both the
    /// symbolic and the C-style form. <c>IMPLIES</c> has a symbolic spelling
    /// (<c>→</c>) but no C-family one, so it keeps its word form in <see cref="OperatorStyle.CStyle"/>; <c>NAND</c>/<c>NOR</c> behave the same way (<c>↑</c>/<c>↓</c>).
    /// </summary>
    private static string StyledLabel(RuleDescription description, OperatorStyle style, bool showArgumentValues)
    {
        string label = BaseLabel(description, style);
        return showArgumentValues && description.ArgumentText is { } argumentText ? $"{label} ({argumentText})" : label;
    }

    private static string BaseLabel(RuleDescription description, OperatorStyle style)
    {
        if (style == OperatorStyle.Word || description.Operands.Count == 0)
        {
            return description.Label;
        }

        return style switch
        {
            OperatorStyle.Symbolic => description.Label switch
            {
                "AND" => "∧",
                "OR" => "∨",
                "NOT" => "¬",
                "XOR" => "⊕",
                "EQUIVALENT" => "↔",
                "IMPLIES" => "→",
                "NAND" => "↑",
                "NOR" => "↓",
                "COALESCE" => "??",
                _ => description.Label,
            },
            OperatorStyle.CStyle => description.Label switch
            {
                "AND" => "&&",
                "OR" => "||",
                "NOT" => "!",
                "XOR" => "^",
                "EQUIVALENT" => "==",
                "COALESCE" => "??",
                _ => description.Label,
            },
            _ => throw new InvalidOperationException($"Unhandled operator style '{style}'."),
        };
    }

    /// <summary>
    /// Guards the positional-zip invariant this method depends on: <see cref="RuleDescription"/> and
    /// <see cref="EvaluatedNode"/> are built by two independent recursive traversals (<c>CompiledRule.DescribeNode</c>
    /// and <see cref="Evaluation.Evaluator{TContext}"/> respectively — see <c>evaluated-node-rule-description-alignment</c>
    /// ticket 02 for making that structurally impossible to violate instead of merely conventional), so
    /// nothing stops them from disagreeing on operand count if either traversal is edited carelessly.
    /// Runs in every build: without it, a mismatch surfaces later as an <see cref="IndexOutOfRangeException"/>
    /// from the positional zip below, which is far harder to diagnose than this guard's explicit message.
    /// </summary>
    private static void AssertOperandCountsAligned(RuleDescription description, EvaluatedNode? evaluated)
    {
        if (evaluated is { NotEvaluated: false } && evaluated.Children.Count != description.Operands.Count)
        {
            throw new InvalidOperationException(
                $"RuleDescription/EvaluatedNode operand-count mismatch at node '{description.Label}': "
                    + $"the description has {description.Operands.Count} operand(s) but the evaluated "
                    + $"tree has {evaluated.Children.Count}. RuleRenderTree.Build zips these two trees "
                    + "positionally, assuming both were built from the same operand order."
            );
        }
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
