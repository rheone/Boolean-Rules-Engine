namespace BooleanRulesEngine.Printing;

using BooleanRulesEngine.Ast;

/// <summary>
/// Renders a compiled expression tree back to canonical DSL text (ADR-0003) — deterministic, with
/// minimal-but-unambiguous parentheses, and <c>XOR</c> always parenthesized regardless of context.
/// This is the exact form <c>parse</c> reproduces a structurally equal tree from (ticket 06).
/// </summary>
internal static class CanonicalPrinter
{
    private enum PrintContext
    {
        Top,
        AndOperand,
        OrOperand,
        NotOperand,
        XorOperand,
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
            (XnorExpression, _) => true,
            (AndExpression, PrintContext.AndOperand or PrintContext.NotOperand or PrintContext.XorOperand) => true,
            (
                OrExpression,
                PrintContext.AndOperand
                    or PrintContext.OrOperand
                    or PrintContext.NotOperand
                    or PrintContext.XorOperand
            ) => true,
            _ => false,
        };
    }

    private static string PrintNode(Expression node, PrintContext context)
    {
        string inner = node switch
        {
            ConstantExpression c => c.Value ? "true" : "false",
            TermExpression t => t.Identity.ToString(),
            NotExpression n => "NOT " + PrintNode(n.Operand, PrintContext.NotOperand),
            AndExpression a => string.Join(" AND ", a.Operands.Select(o => PrintNode(o, PrintContext.AndOperand))),
            OrExpression o => string.Join(" OR ", o.Operands.Select(o2 => PrintNode(o2, PrintContext.OrOperand))),
            XorExpression x => PrintNode(x.Left, PrintContext.XorOperand)
                + " XOR "
                + PrintNode(x.Right, PrintContext.XorOperand),
            XnorExpression xn => PrintNode(xn.Left, PrintContext.XorOperand)
                + " XNOR "
                + PrintNode(xn.Right, PrintContext.XorOperand),
            ExactlyOneExpression e => "ExactlyOne("
                + string.Join(", ", e.Operands.Select(o => PrintNode(o, PrintContext.Top)))
                + ")",
            ThresholdExpression th => $"{th.Comparison}({th.K}, "
                + string.Join(", ", th.Operands.Select(o => PrintNode(o, PrintContext.Top)))
                + ")",
            _ => throw new InvalidOperationException($"Unhandled expression type '{node.GetType()}'."),
        };

        return NeedsWrap(node, context) ? $"({inner})" : inner;
    }
}
