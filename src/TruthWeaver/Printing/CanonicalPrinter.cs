namespace TruthWeaver.Printing;

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
    /// <returns>The canonical DSL text.</returns>
    public static string Print(Expression root)
    {
        return PrintNode(root, PrintContext.Top);
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

    private static string PrintNode(Expression node, PrintContext context)
    {
        string inner = node switch
        {
            ConstantExpression c => TruthValueText.Canonical(c.Value),
            TermExpression t => t.Identity.ToString(),
            NotExpression n => "NOT " + PrintNode(n.Operand, PrintContext.NotOperand),
            AndExpression => JoinOperands(node, " AND ", PrintContext.AndOperand),
            OrExpression => JoinOperands(node, " OR ", PrintContext.OrOperand),
            XorExpression x => PrintNode(x.Left, PrintContext.InfixOperand)
                + " XOR "
                + PrintNode(x.Right, PrintContext.InfixOperand),
            EquivalentExpression xn => PrintNode(xn.Left, PrintContext.InfixOperand)
                + " EQUIVALENT "
                + PrintNode(xn.Right, PrintContext.InfixOperand),
            ImpliesExpression im => PrintNode(im.Antecedent, PrintContext.InfixOperand)
                + " IMPLIES "
                + PrintNode(im.Consequent, PrintContext.InfixOperand),
            NandExpression nd => PrintNode(nd.Left, PrintContext.InfixOperand)
                + " NAND "
                + PrintNode(nd.Right, PrintContext.InfixOperand),
            NorExpression nr => PrintNode(nr.Left, PrintContext.InfixOperand)
                + " NOR "
                + PrintNode(nr.Right, PrintContext.InfixOperand),
            NxorExpression => $"NXOR({JoinOperands(node, ", ", PrintContext.Top)})",
            AnyExpression => $"ANY({JoinOperands(node, ", ", PrintContext.Top)})",
            AllExpression => $"ALL({JoinOperands(node, ", ", PrintContext.Top)})",
            NoneExpression => $"NONE({JoinOperands(node, ", ", PrintContext.Top)})",
            ExactlyOneExpression => $"ExactlyOne({JoinOperands(node, ", ", PrintContext.Top)})",
            IfExpression => $"If({JoinOperands(node, ", ", PrintContext.Top)})",
            CoalesceExpression => $"COALESCE({JoinOperands(node, ", ", PrintContext.Top)})",
            BetweenExpression bt => $"BETWEEN({bt.Min}, {bt.Max}, {JoinOperands(node, ", ", PrintContext.Top)})",
            ThresholdExpression th => $"{th.Comparison}({th.K}, {JoinOperands(node, ", ", PrintContext.Top)})",
            _ => throw new InvalidOperationException($"Unhandled expression type '{node.GetType()}'."),
        };

        return NeedsWrap(node, context) ? $"({inner})" : inner;
    }

    private static string JoinOperands(Expression node, string separator, PrintContext operandContext)
    {
        return string.Join(separator, ExpressionShape.Of(node).Operands.Select(o => PrintNode(o, operandContext)));
    }
}
