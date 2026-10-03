namespace TruthWeaver.Rewriting;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;

/// <summary>
/// Rewrites every derived operator of an <see cref="Expression"/> tree into the primitive kernel
/// (<c>NOT</c>, <c>AND</c>, <c>OR</c>, <c>AtLeast</c>, <c>AtMost</c>, <c>Exactly</c>, <c>COALESCE</c>; ADR-0005 decisions
/// 3, 3a and 10). The input tree is never modified: a new tree is built and unchanged sub-trees are shared.
/// </summary>
/// <remarks>
/// Every definition below is verified exhaustively against the truth-table oracle over all
/// <c>{True, False, Unknown}</c> assignments, not just derived on paper, because several classical identities fail in
/// Strong Kleene logic. Operands that a definition mentions more than once (for example both operands of <c>XOR</c>) are
/// the same shared, already-expanded node, so the rewritten tree is a DAG in memory; only its printed text repeats them.
/// </remarks>
internal static class PrimitiveExpander
{
    /// <summary>Expands <paramref name="node"/> and everything below it into primitive operators.</summary>
    /// <param name="node">The tree to expand.</param>
    /// <returns>An equivalent tree containing only primitive operators, constants and terms.</returns>
    public static Expression Expand(Expression node)
    {
        return node switch
        {
            // Leaves are already primitive.
            ConstantExpression or TermExpression => node,

            // Primitive operators keep their shape; only their operands are expanded.
            NotExpression n => new NotExpression(Expand(n.Operand)),
            AndExpression a => new AndExpression(ExpandAll(a.Operands)),
            OrExpression o => new OrExpression(ExpandAll(o.Operands)),
            CoalesceExpression c => new CoalesceExpression(ExpandAll(c.Operands)),

            // The threshold family: AtLeast, AtMost and Exactly are the kernel; the strict comparisons shift k by one.
            ThresholdExpression t => ExpandThreshold(t),

            // Derived binary operators (ADR-0005 decision 3).
            ImpliesExpression i => Or(Not(Expand(i.Antecedent)), Expand(i.Consequent)),
            XorExpression x => Xor(Expand(x.Left), Expand(x.Right)),
            EquivalentExpression e => Equivalent(Expand(e.Left), Expand(e.Right)),
            NandExpression nd => Not(And(Expand(nd.Left), Expand(nd.Right))),
            NorExpression nr => Not(Or(Expand(nr.Left), Expand(nr.Right))),

            // Parity: True for an odd number of True operands, Unknown if any operand is Unknown (see ExpandParity).
            NxorExpression nx => ExpandParity(ExpandAll(nx.Operands)),

            // Cardinality aliases over the definitely-true / possibly-true interval (ADR-0005 decision 6).
            AnyExpression any => Threshold(ThresholdComparison.AtLeast, 1, ExpandAll(any.Operands)),
            AllExpression all => Threshold(ThresholdComparison.AtLeast, all.Operands.Count, ExpandAll(all.Operands)),
            NoneExpression none => Threshold(ThresholdComparison.AtMost, 0, ExpandAll(none.Operands)),
            ExactlyOneExpression one => Threshold(ThresholdComparison.Exactly, 1, ExpandAll(one.Operands)),
            BetweenExpression b => ExpandBetween(b),

            // Conditional and boundary operators.
            IfExpression f => ExpandIf(Expand(f.Condition), Expand(f.WhenTrue), Expand(f.WhenFalse)),
            ProjectExpression p => Coalesce(Expand(p.Operand), p.UnknownAs ? TruthValue.True : TruthValue.False),
            InspectionExpression s => ExpandInspection(s.Kind, Expand(s.Operand)),

            _ => throw new InvalidOperationException($"Unhandled expression type '{node.GetType().Name}'."),
        };
    }

    private static EquatableArray<Expression> ExpandAll(EquatableArray<Expression> operands)
    {
        return new EquatableArray<Expression>(operands.Select(Expand));
    }

    private static NotExpression Not(Expression operand)
    {
        return new NotExpression(operand);
    }

    private static AndExpression And(params Expression[] operands)
    {
        return new AndExpression(new EquatableArray<Expression>(operands));
    }

    private static OrExpression Or(params Expression[] operands)
    {
        return new OrExpression(new EquatableArray<Expression>(operands));
    }

    private static ThresholdExpression Threshold(ThresholdComparison comparison, int k, EquatableArray<Expression> operands)
    {
        return new ThresholdExpression(comparison, k, operands);
    }

    private static ConstantExpression Constant(TruthValue value)
    {
        return new ConstantExpression(value);
    }

    private static CoalesceExpression Coalesce(Expression operand, TruthValue fallback)
    {
        return new CoalesceExpression(new EquatableArray<Expression>([operand, Constant(fallback)]));
    }

    /// <summary><c>(a AND NOT b) OR (NOT a AND b)</c>: Unknown whenever either operand is.</summary>
    private static OrExpression Xor(Expression left, Expression right)
    {
        return Or(And(left, Not(right)), And(Not(left), right));
    }

    /// <summary><c>(a AND b) OR (NOT a AND NOT b)</c>: the negation of XOR, Unknown whenever either operand is.</summary>
    private static OrExpression Equivalent(Expression left, Expression right)
    {
        return Or(And(left, right), And(Not(left), Not(right)));
    }

    /// <summary>
    /// <c>GreaterThan(k)</c> is <c>AtLeast(k + 1)</c> and <c>LessThan(k)</c> is <c>AtMost(k - 1)</c>: both ask the same
    /// question of every count in the interval, so the equivalence holds for <c>Unknown</c> operands too. The compiler's
    /// valid ranges (<c>GreaterThan</c>: 0..n-1, <c>LessThan</c>: 1..n) map exactly onto the valid ranges of the targets.
    /// </summary>
    private static Expression ExpandThreshold(ThresholdExpression t)
    {
        EquatableArray<Expression> operands = ExpandAll(t.Operands);
        return t.Comparison switch
        {
            ThresholdComparison.GreaterThan => Threshold(ThresholdComparison.AtLeast, t.K + 1, operands),
            ThresholdComparison.LessThan => Threshold(ThresholdComparison.AtMost, t.K - 1, operands),
            _ => Threshold(t.Comparison, t.K, operands),
        };
    }

    /// <summary>
    /// <c>BETWEEN(min, max)</c> is <c>AND(AtLeast(min), AtMost(max))</c>. A bound that does not constrain (<c>min = 0</c>,
    /// <c>max = n</c>) is dropped, because <c>AtLeast(0)</c> / <c>AtMost(n)</c> are structural constants the compiler
    /// rejects; the compiler also rejects both bounds being vacuous, so at least one side always remains.
    /// </summary>
    private static Expression ExpandBetween(BetweenExpression b)
    {
        EquatableArray<Expression> operands = ExpandAll(b.Operands);
        bool hasLower = b.Min > 0;
        bool hasUpper = b.Max < operands.Count;
        if (hasLower && hasUpper)
        {
            return And(
                Threshold(ThresholdComparison.AtLeast, b.Min, operands),
                Threshold(ThresholdComparison.AtMost, b.Max, operands)
            );
        }

        return hasLower
            ? Threshold(ThresholdComparison.AtLeast, b.Min, operands)
            : Threshold(ThresholdComparison.AtMost, b.Max, operands);
    }

    /// <summary>
    /// <c>NXOR</c> is "an odd number of operands are True", and Unknown if any operand is Unknown. That is exactly
    /// <c>OR(Exactly(1), Exactly(3), ...)</c> over the odd counts: with no Unknown operand the interval is a single count
    /// and the disjunction is True iff that count is odd; with at least one Unknown the interval holds two or more
    /// consecutive counts, so every <c>Exactly(k)</c> that can match is Unknown (never True) and at least one odd count is
    /// always inside the interval, which makes the disjunction Unknown. This is linear in the operand count, unlike a fold
    /// of the binary XOR expansion, which repeats its accumulator twice per step and so grows exponentially.
    /// </summary>
    private static Expression ExpandParity(EquatableArray<Expression> operands)
    {
        List<Expression> oddCounts = [];
        for (int k = 1; k <= operands.Count; k += 2)
        {
            oddCounts.Add(Threshold(ThresholdComparison.Exactly, k, operands));
        }

        // Two operands have a single odd count (1), and an OR needs at least two operands.
        return oddCounts.Count == 1 ? oddCounts[0] : new OrExpression(new EquatableArray<Expression>(oddCounts));
    }

    /// <summary>
    /// The multiplexer <c>(c AND t) OR (NOT c AND f)</c> plus the consensus term <c>(t AND f)</c>, the same primitive
    /// definition the oracle uses (ADR-0005 decision 13, k3-conformance 16): an Unknown condition does not guess a branch.
    /// </summary>
    private static OrExpression ExpandIf(Expression condition, Expression whenTrue, Expression whenFalse)
    {
        return Or(And(condition, whenTrue), And(Not(condition), whenFalse), And(whenTrue, whenFalse));
    }

    /// <summary>
    /// The inspections look at the K3 <em>state</em>, which no connective alone can see, but <c>COALESCE</c> can:
    /// <c>COALESCE(x, False)</c> maps Unknown to False and leaves True/False alone. From it:
    /// <c>IsTrue(x) = COALESCE(x, False)</c>; <c>IsFalse(x) = COALESCE(NOT x, False)</c>;
    /// <c>IsUnknown(x) = COALESCE(x, True) AND COALESCE(NOT x, True)</c> (both are True only when x is Unknown: a True x
    /// makes the second False and a False x makes the first False); <c>IsKnown(x) = IsTrue(x) OR IsFalse(x)</c>.
    /// All four therefore expand to the kernel and no inspection is left as a semantic boundary.
    /// </summary>
    private static Expression ExpandInspection(InspectionKind kind, Expression operand)
    {
        return kind switch
        {
            InspectionKind.IsTrue => Coalesce(operand, TruthValue.False),
            InspectionKind.IsFalse => Coalesce(Not(operand), TruthValue.False),
            InspectionKind.IsUnknown => And(Coalesce(operand, TruthValue.True), Coalesce(Not(operand), TruthValue.True)),
            InspectionKind.IsKnown => Or(Coalesce(operand, TruthValue.False), Coalesce(Not(operand), TruthValue.False)),
            _ => throw new InvalidOperationException($"Unhandled inspection kind '{kind}'."),
        };
    }
}
