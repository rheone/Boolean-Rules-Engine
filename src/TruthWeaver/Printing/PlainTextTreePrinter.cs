namespace TruthWeaver.Printing;

using System.Text;
using TruthWeaver.Abstractions;
using TruthWeaver.Evaluation;

/// <summary>
/// Renders a rule's <see cref="RuleDescription"/> tree (from <c>CompiledRule.Describe()</c>) as an
/// indented ASCII tree — for logs, terminals, or any consumer that needs the same structure and
/// evaluation coloring <see cref="MermaidTreePrinter"/> renders, without a diagram viewer. Shares its
/// zip/skip-propagation logic with <see cref="MermaidTreePrinter"/> via <see cref="RuleRenderTree"/>,
/// so "what a skipped subtree looks like" is decided in exactly one place for both formats.
/// </summary>
public static class PlainTextTreePrinter
{
    /// <summary>Prints a rule's structure only, with no evaluation annotations.</summary>
    /// <param name="root">The rule's described tree.</param>
    /// <param name="style">How to render the AND/OR/NOT/XOR/XNOR operator labels. Defaults to <see cref="OperatorStyle.Word"/>.</param>
    /// <returns>The indented tree text.</returns>
    public static string Print(RuleDescription root, OperatorStyle style = OperatorStyle.Word)
    {
        return Print(RuleRenderTree.Build(root, style));
    }

    /// <summary>Prints a rule's structure, annotated by one evaluation's result and short-circuit path.</summary>
    /// <param name="root">The rule's described tree.</param>
    /// <param name="evaluatedTree">The matching <see cref="Decision.EvaluatedTree"/> from that evaluation.</param>
    /// <param name="style">How to render the AND/OR/NOT/XOR/XNOR operator labels. Defaults to <see cref="OperatorStyle.Word"/>.</param>
    /// <returns>The indented tree text.</returns>
    public static string Print(RuleDescription root, EvaluatedNode evaluatedTree, OperatorStyle style = OperatorStyle.Word)
    {
        return Print(RuleRenderTree.Build(root, evaluatedTree, style));
    }

    private static string Print(RenderNode root)
    {
        StringBuilder text = new();
        Write(root, prefix: string.Empty, isRoot: true, isLast: true, text);
        return text.ToString();
    }

    private static void Write(RenderNode node, string prefix, bool isRoot, bool isLast, StringBuilder text)
    {
        if (!isRoot)
        {
            text.Append(prefix).Append(isLast ? "└─ " : "├─ ");
        }

        text.Append(node.Label);
        string? suffix = SuffixFor(node.State);
        if (suffix is not null)
        {
            text.Append(suffix);
        }

        text.Append('\n');

        string childPrefix;
        if (isRoot)
        {
            childPrefix = prefix;
        }
        else
        {
            childPrefix = prefix + (isLast ? "   " : "│  ");
        }

        for (int i = 0; i < node.Children.Count; i++)
        {
            Write(node.Children[i], childPrefix, isRoot: false, isLast: i == node.Children.Count - 1, text);
        }
    }

    private static string? SuffixFor(RenderState state)
    {
        return state switch
        {
            RenderState.True => " [true]",
            RenderState.False => " [false]",
            RenderState.Indeterminate => " [unknown]",
            RenderState.Skipped => " [skipped]",
            RenderState.NoData => null,
            _ => null,
        };
    }
}
