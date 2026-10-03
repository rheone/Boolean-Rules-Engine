namespace TruthWeaver.Ast;

/// <summary>
/// The structural shape of an operator <see cref="Expression"/> node: its canonical op-name, its
/// threshold <c>K</c> (only for <see cref="ThresholdExpression"/> and, as its lower bound, <see cref="BetweenExpression"/>;
/// <see langword="null"/> otherwise), the upper bound <c>Max</c> (only for <see cref="BetweenExpression"/>),
/// and its operand list, in source order.
/// </summary>
/// <param name="OpName">
/// The node's canonical, format-neutral op-name — e.g. <c>"And"</c>, <c>"Xor"</c>, or, for a
/// <see cref="ThresholdExpression"/>, its <see cref="ThresholdComparison"/>'s name (e.g.
/// <c>"AtLeast"</c>). Each consumer maps this to its own format-specific keyword or label; the seam
/// only settles what the name structurally is, not how any one format spells it.
/// </param>
/// <param name="K">The threshold value for a <see cref="ThresholdExpression"/>, or the lower bound (<c>min</c>) for a <see cref="BetweenExpression"/>; otherwise <see langword="null"/>.</param>
/// <param name="Operands">The node's operands, in source order.</param>
/// <param name="Max">The upper bound (<c>max</c>) for a <see cref="BetweenExpression"/>; otherwise <see langword="null"/>.</param>
internal readonly record struct NodeShape(string OpName, int? K, IReadOnlyList<Expression> Operands, int? Max = null);

/// <summary>
/// The single seam every consumer that needs an operator node's op-name/K/operands goes through,
/// instead of each re-deriving that same structural fact with its own independent switch over
/// <see cref="Expression"/> (ADR-0004 amendment: see docs/adr/0004-package-boundaries-and-extensibility.md).
/// Adding a new operator variant only requires adding one case here; every consumer downstream of
/// <see cref="Of"/> picks it up automatically for the structural (op-name/K/operands) part of its job,
/// leaving only its own format-specific rendering or evaluation logic for that new case to add.
/// </summary>
internal static class ExpressionShape
{
    /// <summary>Gets the structural shape of an operator node.</summary>
    /// <param name="node">
    /// The expression node. Must not be a <see cref="ConstantExpression"/> or <see cref="TermExpression"/>
    /// — those are leaves with no operand shape, so callers pattern-match those two cases themselves
    /// before falling back to this method for every other node type.
    /// </param>
    /// <returns>The node's op-name, threshold <c>K</c> (if applicable), and operands.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="node"/> is a <see cref="ConstantExpression"/> or a <see cref="TermExpression"/>.
    /// </exception>
    public static NodeShape Of(Expression node)
    {
        return node switch
        {
            NotExpression n => new NodeShape("Not", null, [n.Operand]),
            AndExpression a => new NodeShape("And", null, a.Operands),
            OrExpression o => new NodeShape("Or", null, o.Operands),
            XorExpression x => new NodeShape("Xor", null, [x.Left, x.Right]),
            EquivalentExpression eq => new NodeShape("Equivalent", null, [eq.Left, eq.Right]),
            NandExpression nd => new NodeShape("Nand", null, [nd.Left, nd.Right]),
            NorExpression nr => new NodeShape("Nor", null, [nr.Left, nr.Right]),
            ImpliesExpression i => new NodeShape("Implies", null, [i.Antecedent, i.Consequent]),
            NxorExpression nx => new NodeShape("Nxor", null, nx.Operands),
            AnyExpression an => new NodeShape("Any", null, an.Operands),
            AllExpression al => new NodeShape("All", null, al.Operands),
            NoneExpression no => new NodeShape("None", null, no.Operands),
            ExactlyOneExpression e => new NodeShape("ExactlyOne", null, e.Operands),
            ThresholdExpression th => new NodeShape(th.Comparison.ToString(), th.K, th.Operands),
            BetweenExpression bt => new NodeShape("Between", bt.Min, bt.Operands, bt.Max),
            InspectionExpression ins => new NodeShape(ins.Kind.ToString(), null, [ins.Operand]),
            IfExpression i3 => new NodeShape("If", null, [i3.Condition, i3.WhenTrue, i3.WhenFalse]),
            CoalesceExpression co => new NodeShape("Coalesce", null, co.Operands),
            ConstantExpression or TermExpression => throw new ArgumentException(
                $"'{node.GetType().Name}' is a leaf with no operand shape — handle it directly instead "
                    + $"of calling {nameof(ExpressionShape)}.{nameof(Of)}.",
                nameof(node)
            ),
            _ => throw new InvalidOperationException($"Unhandled expression type '{node.GetType()}'."),
        };
    }
}
