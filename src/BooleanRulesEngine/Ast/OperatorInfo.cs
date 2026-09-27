namespace BooleanRulesEngine.Ast;

/// <summary>
/// Looks up the <see cref="OperatorDescriptor"/> (label + description) for any operator node in a
/// compiled expression tree. Every operator in the closed set (ADR-0004) has one; a
/// <see cref="TermExpression"/> does not — a term's label/description come from its predicate's
/// registered <see cref="Abstractions.PredicateSchema"/> instead, since that's where they are
/// authored, not from the AST node itself.
/// </summary>
public static class OperatorInfo
{
    /// <summary>Gets the label and description for an operator node.</summary>
    /// <param name="node">The expression node.</param>
    /// <returns>The operator's label and description.</returns>
    /// <exception cref="ArgumentException"><paramref name="node"/> is a <see cref="TermExpression"/>.</exception>
    public static OperatorDescriptor Describe(Expression node)
    {
        return node switch
        {
            ConstantExpression c => c.Value
                ? new OperatorDescriptor("True", "A fixed truth value.")
                : new OperatorDescriptor("False", "A fixed truth value."),
            NotExpression => new OperatorDescriptor("NOT", "Logical negation. Unknown stays Unknown."),
            AndExpression => new OperatorDescriptor(
                "AND",
                "True iff every operand is true. Short-circuits at the first False."
            ),
            OrExpression => new OperatorDescriptor(
                "OR",
                "True iff at least one operand is true. Short-circuits at the first True."
            ),
            XorExpression => new OperatorDescriptor(
                "XOR",
                "True iff exactly one of the two operands is true. Unknown if either operand is Unknown."
            ),
            XnorExpression => new OperatorDescriptor(
                "XNOR",
                "Logical biconditional (IFF) — true iff both operands agree (both true or both false). The negation of XOR."
            ),
            ExactlyOneExpression => new OperatorDescriptor("ExactlyOne", "True iff exactly one operand is true."),
            ThresholdExpression th => new OperatorDescriptor($"{th.Comparison}({th.K})", ThresholdDescription(th)),
            TermExpression => throw new ArgumentException(
                "A TermExpression has no operator descriptor — look up its label/description from the "
                    + "registered PredicateSchema via the term's predicate name instead.",
                nameof(node)
            ),
            _ => throw new InvalidOperationException($"Unhandled expression type '{node.GetType()}'."),
        };
    }

    private static string ThresholdDescription(ThresholdExpression threshold)
    {
        return threshold.Comparison switch
        {
            ThresholdComparison.AtLeast => $"True iff at least {threshold.K} of the operands are true.",
            ThresholdComparison.AtMost => $"True iff at most {threshold.K} of the operands are true.",
            ThresholdComparison.GreaterThan => $"True iff more than {threshold.K} of the operands are true.",
            ThresholdComparison.LessThan => $"True iff fewer than {threshold.K} of the operands are true.",
            ThresholdComparison.Exactly => $"True iff exactly {threshold.K} of the operands are true.",
            _ => throw new InvalidOperationException($"Unhandled threshold comparison '{threshold.Comparison}'."),
        };
    }
}
