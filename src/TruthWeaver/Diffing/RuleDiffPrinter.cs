namespace TruthWeaver.Diffing;

using TruthWeaver.Evaluation;

/// <summary>
/// Renders a <see cref="RuleDiffResult"/> (ticket rule-diff/01) as human-readable text, suitable for an
/// audit-log entry or a rule-review UI — one line per <see cref="RuleDiffEntry"/>, naming each node by
/// its <see cref="RuleDescription.Label"/>/<see cref="RuleDescription.Description"/> rather than its
/// closed-set AST type name (ADR-0004).
/// </summary>
/// <example>
/// Given a "before" rule <c>isManager AND isDepartmentHead</c> and an "after" rule
/// <c>isManager AND isDepartmentHead AND hasRole(role: "Y")</c>:
/// <code>
/// RuleDiffResult diff = RuleDiff.Compare(before, after);
/// string text = RuleDiffPrinter.Print(diff);
/// // "Added operand at root/2: hasRole — Test predicate 'hasRole', true iff 'role' equals 'Y'."
/// </code>
/// </example>
public static class RuleDiffPrinter
{
    /// <summary>Renders a diff as human-readable text, one line per entry.</summary>
    /// <param name="diff">The diff to render.</param>
    /// <returns><c>"No changes."</c> when <paramref name="diff"/> is empty; otherwise one line per entry, newline-separated.</returns>
    public static string Print(RuleDiffResult diff)
    {
        ArgumentNullException.ThrowIfNull(diff);

        return diff.HasChanges ? string.Join('\n', diff.Entries.Select(PrintEntry)) : "No changes.";
    }

    private static string PrintEntry(RuleDiffEntry entry)
    {
        string path = PrintPath(entry.Path);
        return entry.Kind switch
        {
            RuleDiffChangeKind.Added => $"Added operand at {path}: {PrintNode(entry.After)}",
            RuleDiffChangeKind.Removed => $"Removed operand at {path}: {PrintNode(entry.Before)}",
            RuleDiffChangeKind.Changed => $"Changed at {path}: {PrintNode(entry.Before)} -> {PrintNode(entry.After)}",
            _ => throw new InvalidOperationException($"Unhandled diff change kind '{entry.Kind}'."),
        };
    }

    private static string PrintNode(RuleDescription? node)
    {
        return node is null ? "(none)" : $"{node.Label} — {node.Description}";
    }

    private static string PrintPath(IReadOnlyList<int> path)
    {
        return path.Count == 0 ? "root" : "root/" + string.Join('/', path);
    }
}
