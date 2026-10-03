namespace TruthWeaver.Tests.TestSupport;

using TruthWeaver.Evaluation;

/// <summary>
/// An implementation-independent size measure for rewrite tests: the number of nodes in a rule's printed tree. It reads the
/// plain-text tree (one line per node, shared sub-expressions counted every time they appear), so the rewrite code under
/// test cannot also hide a miscount in its own measure.
/// </summary>
public static class RuleMetrics
{
    /// <summary>Counts the nodes of <paramref name="rule"/>'s printed tree.</summary>
    /// <param name="rule">The rule to measure.</param>
    /// <returns>The node count.</returns>
    public static int NodeCount(CompiledRule<RuleTestContext> rule)
    {
        return rule.PrintPlainText().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
    }
}
