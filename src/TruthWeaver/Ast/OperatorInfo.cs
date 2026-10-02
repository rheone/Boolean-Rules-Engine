namespace TruthWeaver.Ast;

/// <summary>
/// Looks up the <see cref="OperatorDescriptor"/> (label + description) for any operator node in a
/// compiled expression tree. Every operator in the closed set (ADR-0004) has one; a
/// <see cref="TermExpression"/> does not — a term's label/description come from its predicate's
/// registered <see cref="Abstractions.PredicateSchema"/> instead, since that's where they are
/// authored, not from the AST node itself.
/// </summary>
public static class OperatorInfo
{
    // TODO operator descriptions should be enriched with context, not be a static value unless a static value is actually called for

    /// <summary>Gets the label and description for an operator node.</summary>
    /// <param name="node">The expression node.</param>
    /// <returns>The operator's label and description.</returns>
    /// <exception cref="ArgumentException"><paramref name="node"/> is a <see cref="TermExpression"/>.</exception>
    public static OperatorDescriptor Describe(Expression node)
    {
        if (node is ConstantExpression c)
        {
            // TODO? Add "Unknown" to possible values / descriptor
            return c.Value
                ? new OperatorDescriptor("True", "A fixed True value.")
                : new OperatorDescriptor("False", "A fixed False value.");
        }

        if (node is TermExpression)
        {
            throw new ArgumentException(
                "A TermExpression has no operator descriptor — look up its label/description from the "
                    + "registered PredicateSchema via the term's predicate name instead.",
                nameof(node)
            );
        }

        NodeShape shape = ExpressionShape.Of(node);
        return shape.OpName switch
        {
            // TODO add all operators

            "Not" => new OperatorDescriptor("NOT", "Logical negation. Unknown stays Unknown."),
            "And" => new OperatorDescriptor("AND", "True iff every operand is true. Short-circuits at the first False."),
            "Or" => new OperatorDescriptor("OR", "True iff at least one operand is true. Short-circuits at the first True."),
            "Xor" => new OperatorDescriptor(
                "XOR",
                "True iff exactly one of the two operands is true. Unknown if either operand is Unknown."
            ),
            "Xnor" => new OperatorDescriptor(
                "XNOR",
                "Logical biconditional (IFF) — true iff both operands agree (both true or both false). The negation of XOR."
            ),
            "ExactlyOne" => new OperatorDescriptor("ExactlyOne", "True iff exactly one operand is true."),
            _ => new OperatorDescriptor($"{shape.OpName}({shape.K})", ThresholdDescription(shape)),
        };
    }

    private static string ThresholdDescription(NodeShape threshold)
    {
        return threshold.OpName switch
        {
            "AtLeast" => $"True iff at least {threshold.K} of the operands are true.",
            "AtMost" => $"True iff at most {threshold.K} of the operands are true.",
            "GreaterThan" => $"True iff more than {threshold.K} of the operands are true.",
            "LessThan" => $"True iff fewer than {threshold.K} of the operands are true.",
            "Exactly" => $"True iff exactly {threshold.K} of the operands are true.",
            _ => throw new InvalidOperationException($"Unhandled threshold comparison '{threshold.OpName}'."),
        };
    }
}
