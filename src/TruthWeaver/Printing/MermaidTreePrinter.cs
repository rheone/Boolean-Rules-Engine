namespace TruthWeaver.Printing;

using System.Text;
using TruthWeaver.Abstractions;
using TruthWeaver.Evaluation;

/// <summary>
/// Renders a rule's <see cref="RuleDescription"/> tree (from <c>CompiledRule.Describe()</c>) as
/// Mermaid <c>flowchart</c> syntax, for delivery to a UI that renders Mermaid diagrams. Optionally
/// colored by one evaluation's <see cref="Decision.EvaluatedTree"/>, via the shared
/// <see cref="RuleRenderTree"/>.
/// </summary>
public static class MermaidTreePrinter
{
    /// <summary>Prints a rule's structure only, with no evaluation coloring.</summary>
    /// <param name="root">The rule's described tree.</param>
    /// <param name="style">How to render the AND/OR/NOT/XOR/XNOR operator labels. Defaults to <see cref="OperatorStyle.Word"/>.</param>
    /// <returns>Mermaid <c>flowchart</c> text.</returns>
    public static string Print(RuleDescription root, OperatorStyle style = OperatorStyle.Word)
    {
        return Print(RuleRenderTree.Build(root, style));
    }

    /// <summary>Prints a rule's structure, colored by one evaluation's result and short-circuit path.</summary>
    /// <param name="root">The rule's described tree.</param>
    /// <param name="evaluatedTree">The matching <see cref="Decision.EvaluatedTree"/> from that evaluation.</param>
    /// <param name="style">How to render the AND/OR/NOT/XOR/XNOR operator labels. Defaults to <see cref="OperatorStyle.Word"/>.</param>
    /// <returns>Mermaid <c>flowchart</c> text.</returns>
    public static string Print(RuleDescription root, EvaluatedNode evaluatedTree, OperatorStyle style = OperatorStyle.Word)
    {
        return Print(RuleRenderTree.Build(root, evaluatedTree, style));
    }

    private static string Print(RenderNode root)
    {
        StringBuilder text = new();
        text.Append("flowchart TD\n");
        List<string> classAssignments = [];
        int nextId = 0;
        bool anyColored = WriteNode(root, text, classAssignments, ref nextId) != RenderState.NoData;

        foreach (string assignment in classAssignments)
        {
            text.Append(assignment).Append('\n');
        }

        if (anyColored || classAssignments.Count > 0)
        {
            WriteClassDefs(text);
        }

        return text.ToString();
    }

    private static RenderState WriteNode(RenderNode node, StringBuilder text, List<string> classAssignments, ref int nextId)
    {
        string id = $"n{nextId++}";
        text.Append("    ").Append(id).Append("[\"").Append(Escape(node.Label)).Append("\"]\n");

        string? className = ClassFor(node.State);
        if (className is not null)
        {
            classAssignments.Add($"    class {id} {className}");
        }

        foreach (RenderNode child in node.Children)
        {
            int childId = nextId;
            WriteNode(child, text, classAssignments, ref nextId);
            text.Append("    ").Append(id).Append(" --> n").Append(childId).Append('\n');
        }

        return node.State;
    }

    private static string? ClassFor(RenderState state)
    {
        return state switch
        {
            RenderState.True => "brTrue",
            RenderState.False => "brFalse",
            RenderState.Indeterminate => "brUnknown",
            RenderState.Skipped => "brSkipped",
            RenderState.NoData => null,
            _ => null,
        };
    }

    private static void WriteClassDefs(StringBuilder text)
    {
        text.Append("    classDef brTrue fill:#d4edda,stroke:#28a745,color:#155724;\n");
        text.Append("    classDef brFalse fill:#f8d7da,stroke:#dc3545,color:#721c24;\n");
        text.Append("    classDef brUnknown fill:#fff3cd,stroke:#ffc107,color:#856404;\n");
        text.Append("    classDef brSkipped fill:#e9ecef,stroke:#adb5bd,color:#6c757d,stroke-dasharray: 4 3;\n");
    }

    private static string Escape(string label)
    {
        return label.Replace("\"", "#quot;").Replace("\r", " ").Replace("\n", " ");
    }
}
