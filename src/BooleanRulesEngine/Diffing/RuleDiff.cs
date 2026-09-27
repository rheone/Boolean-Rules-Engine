namespace BooleanRulesEngine.Diffing;

using BooleanRulesEngine.Ast;
using BooleanRulesEngine.Evaluation;

/// <summary>
/// Computes a structural diff between two compiled rules: which operator, term, or constant nodes were
/// added, removed, or changed between a "before" and an "after" tree, located by operand-index path.
/// Built on <see cref="CompiledRule{TContext}.Describe"/> so every diff entry carries each node's
/// human-readable <see cref="RuleDescription"/> alongside its structural position, without the caller
/// needing to re-derive it from the closed-set AST types (ADR-0004).
/// </summary>
public static class RuleDiff
{
    /// <summary>Compares two compiled rules and returns their structural differences.</summary>
    /// <typeparam name="TContext">The application context type both rules were compiled for.</typeparam>
    /// <param name="before">The rule to diff from.</param>
    /// <param name="after">The rule to diff to.</param>
    /// <returns>
    /// The structural diff, in tree order. Empty (<see cref="RuleDiffResult.HasChanges"/> is
    /// <see langword="false"/>) when <paramref name="before"/> and <paramref name="after"/> are
    /// structurally identical.
    /// </returns>
    public static RuleDiffResult Compare<TContext>(CompiledRule<TContext> before, CompiledRule<TContext> after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        List<RuleDiffEntry> entries = [];
        DiffNode(before.Root, before.Describe(), after.Root, after.Describe(), [], entries);
        return new RuleDiffResult(entries);
    }

    private static void DiffNode(
        Expression before,
        RuleDescription beforeDescription,
        Expression after,
        RuleDescription afterDescription,
        IReadOnlyList<int> path,
        List<RuleDiffEntry> entries
    )
    {
        if (before.Equals(after))
        {
            return;
        }

        if (!SameShape(before, after) || before is TermExpression or ConstantExpression)
        {
            // Either the node kind itself differs (different operator, different threshold K, a term
            // where the other tree has a constant, etc.), or both sides are leaves whose values differ
            // (Equals above already ruled out "equal"). Either way, report the whole node as replaced
            // rather than diffing further into a subtree that no longer corresponds to anything.
            entries.Add(new RuleDiffEntry(RuleDiffChangeKind.Changed, path, beforeDescription, afterDescription));
            return;
        }

        IReadOnlyList<Expression> beforeOperands = ExpressionShape.Of(before).Operands;
        IReadOnlyList<Expression> afterOperands = ExpressionShape.Of(after).Operands;

        int shared = Math.Min(beforeOperands.Count, afterOperands.Count);
        for (int i = 0; i < shared; i++)
        {
            DiffNode(
                beforeOperands[i],
                beforeDescription.Operands[i],
                afterOperands[i],
                afterDescription.Operands[i],
                [.. path, i],
                entries
            );
        }

        for (int i = shared; i < beforeOperands.Count; i++)
        {
            entries.Add(new RuleDiffEntry(RuleDiffChangeKind.Removed, [.. path, i], beforeDescription.Operands[i], null));
        }

        for (int i = shared; i < afterOperands.Count; i++)
        {
            entries.Add(new RuleDiffEntry(RuleDiffChangeKind.Added, [.. path, i], null, afterDescription.Operands[i]));
        }
    }

    private static bool SameShape(Expression before, Expression after)
    {
        if (before.GetType() != after.GetType())
        {
            return false;
        }

        if (before is ThresholdExpression beforeThreshold && after is ThresholdExpression afterThreshold)
        {
            return beforeThreshold.Comparison == afterThreshold.Comparison && beforeThreshold.K == afterThreshold.K;
        }

        return true;
    }
}
