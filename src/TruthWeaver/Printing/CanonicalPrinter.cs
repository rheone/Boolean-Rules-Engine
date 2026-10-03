namespace TruthWeaver.Printing;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;

/// <summary>
/// Renders a compiled expression tree back to canonical DSL text (ADR-0003) — deterministic, and
/// parenthesized for clarity wherever an operator is mixed with a different one, even where
/// precedence alone would make the meaning unambiguous (e.g. <c>a AND b OR c</c> prints as
/// <c>(a AND b) OR c</c>) — the point is to make a large nested rule easy for a human to read at a
/// glance, not merely to avoid a parser error. <c>XOR</c>/<c>EQUIVALENT</c>/<c>IMPLIES</c>/<c>NAND</c>/<c>NOR</c> are always parenthesized
/// regardless of context. This is the exact form <c>parse</c> reproduces a structurally equal tree
/// from (ticket 06).
/// </summary>
internal static class CanonicalPrinter
{
    private enum PrintContext
    {
        Top,
        AndOperand,
        OrOperand,
        NotOperand,
        InfixOperand,
    }

    /// <summary>Prints an expression tree to canonical DSL text.</summary>
    /// <param name="root">The tree to print.</param>
    /// <param name="collapse">
    /// The outermost <c>Collapse</c> policy the rule declared, or <see langword="null"/>. When present it wraps the whole
    /// tree (<c>Collapse(expr, UnknownAsFalse)</c>), the only place the DSL accepts it.
    /// </param>
    /// <param name="grouping">
    /// The grouping delimiters to wrap operands in. <see cref="GroupingStyle.Parentheses"/> (the default) is the canonical
    /// form; <see cref="GroupingStyle.DepthCycling"/> prints the same tree with the delimiter chosen by group depth.
    /// </param>
    /// <returns>The DSL text.</returns>
    public static string Print(
        Expression root,
        CollapsePolicy? collapse = null,
        GroupingStyle grouping = GroupingStyle.Parentheses
    )
    {
        string text = PrintNode(root, PrintContext.Top, 0, grouping);
        return collapse is { } policy ? $"Collapse({text}, {CollapsePolicyText.Canonical(policy)})" : text;
    }

    private static bool NeedsWrap(Expression node, PrintContext context)
    {
        return (node, context) switch
        {
            (XorExpression, _) => true,
            (EquivalentExpression, _) => true,
            (ImpliesExpression, _) => true,
            (NandExpression, _) => true,
            (NorExpression, _) => true,
            (
                AndExpression,
                PrintContext.AndOperand
                    or PrintContext.OrOperand
                    or PrintContext.NotOperand
                    or PrintContext.InfixOperand
            ) => true,
            (
                OrExpression,
                PrintContext.AndOperand
                    or PrintContext.OrOperand
                    or PrintContext.NotOperand
                    or PrintContext.InfixOperand
            ) => true,
            _ => false,
        };
    }

    private static string PrintNode(Expression node, PrintContext context, int depth, GroupingStyle grouping)
    {
        // A node that gets wrapped opens a group, so its operands sit one group deeper than the node itself.
        bool wrap = NeedsWrap(node, context);
        int innerDepth = wrap ? depth + 1 : depth;
        string inner = node switch
        {
            ConstantExpression c => TruthValueText.Canonical(c.Value),
            TermExpression t => t.Identity.ToString(),
            NotExpression n => "NOT " + PrintNode(n.Operand, PrintContext.NotOperand, innerDepth, grouping),
            AndExpression => JoinOperands(node, " AND ", PrintContext.AndOperand, innerDepth, grouping),
            OrExpression => JoinOperands(node, " OR ", PrintContext.OrOperand, innerDepth, grouping),
            XorExpression x => PrintNode(x.Left, PrintContext.InfixOperand, innerDepth, grouping)
                + " XOR "
                + PrintNode(x.Right, PrintContext.InfixOperand, innerDepth, grouping),
            EquivalentExpression xn => PrintNode(xn.Left, PrintContext.InfixOperand, innerDepth, grouping)
                + " EQUIVALENT "
                + PrintNode(xn.Right, PrintContext.InfixOperand, innerDepth, grouping),
            ImpliesExpression im => PrintNode(im.Antecedent, PrintContext.InfixOperand, innerDepth, grouping)
                + " IMPLIES "
                + PrintNode(im.Consequent, PrintContext.InfixOperand, innerDepth, grouping),
            NandExpression nd => PrintNode(nd.Left, PrintContext.InfixOperand, innerDepth, grouping)
                + " NAND "
                + PrintNode(nd.Right, PrintContext.InfixOperand, innerDepth, grouping),
            NorExpression nr => PrintNode(nr.Left, PrintContext.InfixOperand, innerDepth, grouping)
                + " NOR "
                + PrintNode(nr.Right, PrintContext.InfixOperand, innerDepth, grouping),
            NxorExpression => $"NXOR({JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping)})",
            AnyExpression => $"ANY({JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping)})",
            AllExpression => $"ALL({JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping)})",
            NoneExpression => $"NONE({JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping)})",
            ExactlyOneExpression => $"ExactlyOne({JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping)})",
            ProjectExpression pr =>
                $"Project({PrintNode(pr.Operand, PrintContext.Top, innerDepth, grouping)}, {TruthValueText.Canonical(pr.UnknownAs)})",
            InspectionExpression ins => $"{ins.Kind}({PrintNode(ins.Operand, PrintContext.Top, innerDepth, grouping)})",
            IfExpression => $"If({JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping)})",
            CoalesceExpression => $"COALESCE({JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping)})",
            BetweenExpression bt =>
                $"BETWEEN({bt.Min}, {bt.Max}, {JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping)})",
            ThresholdExpression th =>
                $"{th.Comparison}({th.K}, {JoinOperands(node, ", ", PrintContext.Top, innerDepth, grouping)})",
            _ => throw new InvalidOperationException($"Unhandled expression type '{node.GetType()}'."),
        };

        if (!wrap)
        {
            return inner;
        }

        (char open, char close) = Delimiters(depth, grouping);
        return $"{open}{inner}{close}";
    }

    /// <summary>
    /// Picks the delimiter pair for a group at <paramref name="depth"/>: always parentheses, or cycling <c>(</c> <c>[</c>
    /// <c>{</c> by depth so each nesting level looks different. Depth, not position or content, decides, so the output is
    /// deterministic and independent of what the operands mean.
    /// </summary>
    private static (char Open, char Close) Delimiters(int depth, GroupingStyle grouping)
    {
        if (grouping == GroupingStyle.Parentheses)
        {
            return ('(', ')');
        }

        return (depth % 3) switch
        {
            0 => ('(', ')'),
            1 => ('[', ']'),
            _ => ('{', '}'),
        };
    }

    private static string JoinOperands(
        Expression node,
        string separator,
        PrintContext operandContext,
        int depth,
        GroupingStyle grouping
    )
    {
        return string.Join(
            separator,
            ExpressionShape.Of(node).Operands.Select(o => PrintNode(o, operandContext, depth, grouping))
        );
    }
}
