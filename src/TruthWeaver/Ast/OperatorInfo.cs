namespace TruthWeaver.Ast;

using TruthWeaver.Abstractions;

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
            string label = TruthValueText.Canonical(c.Value);
            return new OperatorDescriptor(label, $"A fixed {label} value.");
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
            "Equivalent" => new OperatorDescriptor(
                "EQUIVALENT",
                "Logical biconditional (IFF, formerly XNOR) — true iff both operands agree (both true or both false). The negation of XOR; Unknown if either operand is Unknown."
            ),
            "Implies" => new OperatorDescriptor(
                "IMPLIES",
                "Material implication (NOT antecedent OR consequent). True if the antecedent is False or the consequent is True; otherwise Unknown if either operand is Unknown."
            ),
            "Nand" => new OperatorDescriptor(
                "NAND",
                "Negated conjunction, NOT (left AND right). False only when both operands are True; True if either is False; otherwise Unknown."
            ),
            "Nor" => new OperatorDescriptor(
                "NOR",
                "Negated disjunction, NOT (left OR right). True only when both operands are False; False if either is True; otherwise Unknown."
            ),
            "Nxor" => new OperatorDescriptor(
                "NXOR",
                "N-ary parity. True iff an odd number of operands are true and none is Unknown; False iff an even number are true and none is Unknown; Unknown whenever any operand is Unknown."
            ),
            "Any" => new OperatorDescriptor(
                "ANY",
                "True iff at least one operand is true (AtLeast(1, ...)). False iff every operand is false; Unknown when no operand is true yet some are Unknown."
            ),
            "All" => new OperatorDescriptor(
                "ALL",
                "True iff every operand is true (AtLeast(n, ...)). False iff any operand is false; Unknown when no operand is false yet some are Unknown."
            ),
            "None" => new OperatorDescriptor(
                "NONE",
                "True iff no operand is true (AtMost(0, ...)). False iff any operand is true; Unknown when no operand is true yet some are Unknown."
            ),
            "Between" => new OperatorDescriptor(
                $"BETWEEN({shape.K}, {shape.Max})",
                $"True iff the number of true operands is between {shape.K} and {shape.Max} inclusive (AtLeast({shape.K}, ...) AND AtMost({shape.Max}, ...)). False iff no possible count of true operands lies in that range; Unknown otherwise."
            ),
            "Coalesce" => new OperatorDescriptor(
                "COALESCE",
                "The first operand that is not Unknown; True and False pass through unchanged and the result is Unknown only if every operand is Unknown. Operands after the first known value are skipped."
            ),
            "IsTrue" => new OperatorDescriptor(
                "IsTrue",
                "Inspection: True iff the operand is True; False if it is False or Unknown. The result is never Unknown, so it never collapses the enclosing rule."
            ),
            "IsFalse" => new OperatorDescriptor(
                "IsFalse",
                "Inspection: True iff the operand is False; False if it is True or Unknown. The result is never Unknown, so it never collapses the enclosing rule."
            ),
            "IsUnknown" => new OperatorDescriptor(
                "IsUnknown",
                "Inspection: True iff the operand is Unknown; False if it is True or False. The result is never Unknown, so it never collapses the enclosing rule."
            ),
            "IsKnown" => new OperatorDescriptor(
                "IsKnown",
                "Inspection: True iff the operand is True or False; False if it is Unknown. The result is never Unknown, so it never collapses the enclosing rule."
            ),
            "Project" => new OperatorDescriptor(
                $"Project({TruthValueText.Canonical(shape.UnknownAs == true)})",
                $"Projection: True and False pass through unchanged and Unknown becomes {TruthValueText.Canonical(shape.UnknownAs == true)}. The result is never Unknown (the same as COALESCE with that value), so it does not collapse the enclosing rule."
            ),
            "If" => new OperatorDescriptor(
                "If",
                "Conditional: the second operand when the condition is True, the third when it is False. An Unknown condition does not pick a branch: the result is the branch value when both branches are the same definite value, otherwise Unknown. Only the needed branch is evaluated for a definite condition."
            ),
            "ExactlyOne" => new OperatorDescriptor("ExactlyOne", "True iff exactly one operand is true."),
            _ => new OperatorDescriptor($"{shape.OpName}({shape.K})", ThresholdDescription(shape)),
        };
    }

    /// <summary>
    /// Gets the label and description for the outermost <c>Collapse</c> boundary a rule declared. It is not an
    /// <see cref="Expression"/> node, so it is described here rather than through <see cref="Describe"/>.
    /// </summary>
    /// <param name="policy">The declared policy.</param>
    /// <returns>The boundary's label (<c>Collapse(UnknownAsFalse)</c>, ...) and description.</returns>
    internal static OperatorDescriptor DescribeCollapse(CollapsePolicy policy)
    {
        string meaning = policy switch
        {
            CollapsePolicy.UnknownAsFalse => "Unknown becomes False, so only a definite True is accepted.",
            CollapsePolicy.UnknownAsTrue => "Unknown becomes True, so only a definite False is refused.",
            CollapsePolicy.UnknownIsError =>
                "Unknown becomes a rejected-unresolved outcome (not a fault): the rule could not be resolved either way.",
            _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unhandled collapse policy."),
        };
        return new OperatorDescriptor(
            $"Collapse({CollapsePolicyText.Canonical(policy)})",
            $"Final boundary: turns the three-valued result of the rule into a two-valued answer. True and False pass through; {meaning}"
        );
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
