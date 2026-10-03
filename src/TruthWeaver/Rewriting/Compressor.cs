namespace TruthWeaver.Rewriting;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;

/// <summary>
/// Rewrites primitive shapes back into readable derived operators (ADR-0005 decision 10): the inverse direction of
/// <see cref="PrimitiveExpander"/>. Every pattern below is an identity of Strong Kleene logic (verified against the
/// truth-table oracle by the property tests), never a classical-only law, and every replacement has no more nodes than
/// the shape it replaces, so the result is never larger than the input.
/// </summary>
/// <remarks>
/// Matching is top-down so a composite pattern (for example the <c>XOR</c> shape) is recognised before its parts are
/// rewritten into something else; a pass is repeated until nothing changes so that a shape exposed by a rewrite below it
/// (a range that appears only after a pair is merged) is recognised too, which also makes the result idempotent.
/// </remarks>
internal static class Compressor
{
    /// <summary>
    /// A safety bound on the repeat-until-stable loop. Every rewrite shrinks the tree or moves a node to a derived form no
    /// pattern consumes, so passes converge in a handful of rounds; the bound only guards against a future pattern cycling.
    /// </summary>
    private const int MaxPasses = 16;

    /// <summary>Compresses <paramref name="root"/> and everything below it.</summary>
    /// <param name="root">The tree to compress.</param>
    /// <returns>An equivalent tree, no larger than <paramref name="root"/>, using derived operators where patterns matched.</returns>
    public static Expression Compress(Expression root)
    {
        Expression current = root;
        for (int pass = 0; pass < MaxPasses; pass++)
        {
            Expression next = new Pass().Visit(current);
            if (next.Equals(current))
            {
                return current;
            }

            current = next;
        }

        return current;
    }

    /// <summary>One top-down sweep. The memo keeps shared sub-trees shared and stops a repeated operand being rewritten twice.</summary>
    private sealed class Pass
    {
        private readonly Dictionary<Expression, Expression> memo = new(ReferenceEqualityComparer.Instance);

        public Expression Visit(Expression node)
        {
            if (this.memo.TryGetValue(node, out Expression? known))
            {
                return known;
            }

            Expression result = this.Match(node) ?? ExpressionTools.MapChildren(node, this.Visit);
            this.memo[node] = result;
            return result;
        }

        /// <summary><c>XOR(l, r) = (l AND NOT r) OR (NOT l AND r)</c>.</summary>
        private static bool IsXor(Expression first, Expression second, out Expression? left, out Expression? right)
        {
            left = null;
            right = null;
            if (
                first is AndExpression { Operands: { Count: 2 } a }
                && second is AndExpression { Operands: { Count: 2 } b }
                && a[1] is NotExpression notRight
                && b[0] is NotExpression notLeft
                && notLeft.Operand.Equals(a[0])
                && notRight.Operand.Equals(b[1])
            )
            {
                left = a[0];
                right = b[1];
                return true;
            }

            return false;
        }

        /// <summary><c>EQUIVALENT(l, r) = (l AND r) OR (NOT l AND NOT r)</c>.</summary>
        private static bool IsEquivalent(Expression first, Expression second, out Expression? left, out Expression? right)
        {
            left = null;
            right = null;
            if (
                first is AndExpression { Operands: { Count: 2 } a }
                && second is AndExpression { Operands: { Count: 2 } b }
                && b[0] is NotExpression notLeft
                && b[1] is NotExpression notRight
                && notLeft.Operand.Equals(a[0])
                && notRight.Operand.Equals(a[1])
            )
            {
                left = a[0];
                right = a[1];
                return true;
            }

            return false;
        }

        /// <summary><c>If(c, t, f) = (c AND t) OR (NOT c AND f) OR (t AND f)</c> (the multiplexer plus the consensus term).</summary>
        private static bool IsIf(
            EquatableArray<Expression> ops,
            out Expression? condition,
            out Expression? whenTrue,
            out Expression? whenFalse
        )
        {
            condition = null;
            whenTrue = null;
            whenFalse = null;
            if (
                ops[0] is AndExpression { Operands: { Count: 2 } first }
                && ops[1] is AndExpression { Operands: { Count: 2 } second }
                && ops[2] is AndExpression { Operands: { Count: 2 } consensus }
                && second[0] is NotExpression negated
                && negated.Operand.Equals(first[0])
                && consensus[0].Equals(first[1])
                && consensus[1].Equals(second[1])
            )
            {
                condition = first[0];
                whenTrue = first[1];
                whenFalse = second[1];
                return true;
            }

            return false;
        }

        /// <summary>
        /// <c>COALESCE(x, fallback)</c> beside <c>COALESCE(NOT x, fallback)</c>: the two halves of <c>IsKnown</c> (fallback
        /// <c>False</c>) and <c>IsUnknown</c> (fallback <c>True</c>).
        /// </summary>
        private static bool IsInspectionPair(Expression first, Expression second, bool falseFallback, out Expression? operand)
        {
            operand = null;
            TruthValue fallback = falseFallback ? TruthValue.False : TruthValue.True;
            if (
                first is CoalesceExpression { Operands: { Count: 2 } a }
                && second is CoalesceExpression { Operands: { Count: 2 } b }
                && a[1] is ConstantExpression { Value: var fa }
                && b[1] is ConstantExpression { Value: var fb }
                && fa == fallback
                && fb == fallback
                && b[0] is NotExpression negated
                && negated.Operand.Equals(a[0])
            )
            {
                operand = a[0];
                return true;
            }

            return false;
        }

        /// <summary>
        /// <c>OR(Exactly(1, ops), Exactly(3, ops), ...)</c> over every odd count up to <c>n</c> is the expansion of
        /// <c>NXOR(ops)</c> (see <see cref="PrimitiveExpander"/>). It needs at least three operands: two operands expand to a
        /// single <c>Exactly(1)</c>, which is not an <c>OR</c>.
        /// </summary>
        private static bool IsParity(EquatableArray<Expression> ops, out EquatableArray<Expression> operands)
        {
            operands = default;
            if (ops[0] is not ThresholdExpression { Comparison: ThresholdComparison.Exactly, K: 1 } first)
            {
                return false;
            }

            int n = first.Operands.Count;
            if (n < 3 || ops.Count != (n + 1) / 2)
            {
                return false;
            }

            for (int i = 0; i < ops.Count; i++)
            {
                if (
                    ops[i] is not ThresholdExpression { Comparison: ThresholdComparison.Exactly } term
                    || term.K != (2 * i) + 1
                    || !term.Operands.Equals(first.Operands)
                )
                {
                    return false;
                }
            }

            operands = first.Operands;
            return true;
        }

        private Expression? Match(Expression node)
        {
            return node switch
            {
                OrExpression o => this.MatchOr(o),
                AndExpression a => this.MatchAnd(a),
                NotExpression n => this.MatchNot(n),
                ThresholdExpression t => this.MatchThreshold(t),
                CoalesceExpression c => this.MatchCoalesce(c),
                _ => null,
            };
        }

        private Expression? MatchOr(OrExpression or)
        {
            EquatableArray<Expression> ops = or.Operands;
            if (ops.Count == 2)
            {
                // XOR and EQUIVALENT expand to a two-term OR of two-term ANDs (see PrimitiveExpander); OR is commutative.
                if (IsXor(ops[0], ops[1], out Expression? xl, out Expression? xr) || IsXor(ops[1], ops[0], out xl, out xr))
                {
                    return new XorExpression(this.Visit(xl!), this.Visit(xr!));
                }

                if (
                    IsEquivalent(ops[0], ops[1], out Expression? el, out Expression? er)
                    || IsEquivalent(ops[1], ops[0], out el, out er)
                )
                {
                    return new EquivalentExpression(this.Visit(el!), this.Visit(er!));
                }

                // IsKnown(x) = COALESCE(x, False) OR COALESCE(NOT x, False).
                if (
                    IsInspectionPair(ops[0], ops[1], falseFallback: true, out Expression? known)
                    || IsInspectionPair(ops[1], ops[0], true, out known)
                )
                {
                    return new InspectionExpression(InspectionKind.IsKnown, this.Visit(known!));
                }

                // NOT a OR b is a implication; with both sides negated it is a NAND (De Morgan holds in K3).
                if (ops[0] is NotExpression na && ops[1] is NotExpression nb)
                {
                    return new NandExpression(this.Visit(na.Operand), this.Visit(nb.Operand));
                }

                if (ops[0] is NotExpression antecedent)
                {
                    return new ImpliesExpression(this.Visit(antecedent.Operand), this.Visit(ops[1]));
                }

                if (ops[1] is NotExpression swapped)
                {
                    return new ImpliesExpression(this.Visit(swapped.Operand), this.Visit(ops[0]));
                }
            }

            if (ops.Count == 3 && IsIf(ops, out Expression? c, out Expression? t, out Expression? f))
            {
                return new IfExpression(this.Visit(c!), this.Visit(t!), this.Visit(f!));
            }

            return IsParity(ops, out EquatableArray<Expression> parityOperands)
                ? new NxorExpression(ExpressionTools.Array(parityOperands.Select(this.Visit)))
                : null;
        }

        private Expression? MatchAnd(AndExpression and)
        {
            EquatableArray<Expression> ops = and.Operands;
            if (ops.Count == 2)
            {
                if (ops[0] is NotExpression na && ops[1] is NotExpression nb)
                {
                    return new NorExpression(this.Visit(na.Operand), this.Visit(nb.Operand));
                }

                // IsUnknown(x) = COALESCE(x, True) AND COALESCE(NOT x, True).
                if (
                    IsInspectionPair(ops[0], ops[1], falseFallback: false, out Expression? unknown)
                    || IsInspectionPair(ops[1], ops[0], false, out unknown)
                )
                {
                    return new InspectionExpression(InspectionKind.IsUnknown, this.Visit(unknown!));
                }
            }

            return this.MergeRange(ops);
        }

        /// <summary>
        /// <c>AND(AtLeast(m, ops), AtMost(M, ops))</c> over the same operands is <c>BETWEEN(m, M, ops)</c> by definition
        /// (ADR-0005 decision 6). <c>m &lt;= M</c> keeps the bounds a valid <c>BETWEEN</c>: the threshold ranges the compiler
        /// enforces already guarantee <c>1 &lt;= m</c> and <c>M &lt;= n - 1</c>, so the full range can never appear.
        /// </summary>
        private Expression? MergeRange(EquatableArray<Expression> ops)
        {
            for (int i = 0; i < ops.Count; i++)
            {
                if (ops[i] is not ThresholdExpression { Comparison: ThresholdComparison.AtLeast } lower)
                {
                    continue;
                }

                for (int j = 0; j < ops.Count; j++)
                {
                    if (
                        ops[j] is ThresholdExpression { Comparison: ThresholdComparison.AtMost } upper
                        && upper.Operands.Equals(lower.Operands)
                        && lower.K <= upper.K
                    )
                    {
                        Expression between = new BetweenExpression(
                            lower.K,
                            upper.K,
                            ExpressionTools.Array(lower.Operands.Select(this.Visit))
                        );
                        if (ops.Count == 2)
                        {
                            return between;
                        }

                        List<Expression> rest = [];
                        for (int k = 0; k < ops.Count; k++)
                        {
                            if (k == i)
                            {
                                rest.Add(between);
                            }
                            else if (k != j)
                            {
                                rest.Add(this.Visit(ops[k]));
                            }
                        }

                        return new AndExpression(ExpressionTools.Array(rest));
                    }
                }
            }

            return null;
        }

        private Expression? MatchNot(NotExpression not)
        {
            // count < k+1 is the negation of count >= k+1 for every count in the interval, so NOT flips a threshold comparison.
            return not.Operand switch
            {
                AndExpression { Operands.Count: 2 } and => new NandExpression(
                    this.Visit(and.Operands[0]),
                    this.Visit(and.Operands[1])
                ),
                OrExpression { Operands.Count: 2 } or => new NorExpression(
                    this.Visit(or.Operands[0]),
                    this.Visit(or.Operands[1])
                ),
                ThresholdExpression { Comparison: ThresholdComparison.AtLeast } atLeast => new ThresholdExpression(
                    ThresholdComparison.AtMost,
                    atLeast.K - 1,
                    ExpressionTools.Array(atLeast.Operands.Select(this.Visit))
                ),
                ThresholdExpression { Comparison: ThresholdComparison.AtMost } atMost => new ThresholdExpression(
                    ThresholdComparison.AtLeast,
                    atMost.K + 1,
                    ExpressionTools.Array(atMost.Operands.Select(this.Visit))
                ),
                _ => null,
            };
        }

        private Expression? MatchThreshold(ThresholdExpression t)
        {
            int n = t.Operands.Count;
            if (n < 2)
            {
                // ANY/ALL/NONE/ExactlyOne need two or more operands.
                return null;
            }

            EquatableArray<Expression> ops = ExpressionTools.Array(t.Operands.Select(this.Visit));
            return (t.Comparison, t.K) switch
            {
                (ThresholdComparison.AtLeast, 1) => new AnyExpression(ops),
                (ThresholdComparison.AtLeast, _) when t.K == n => new AllExpression(ops),
                (ThresholdComparison.AtMost, 0) => new NoneExpression(ops),
                (ThresholdComparison.Exactly, 1) => new ExactlyOneExpression(ops),
                _ => null,
            };
        }

        private Expression? MatchCoalesce(CoalesceExpression c)
        {
            if (c.Operands.Count != 2 || c.Operands[1] is not ConstantExpression { Value: not TruthValue.Unknown } fallback)
            {
                return null;
            }

            // COALESCE(NOT x, False) is exactly IsFalse(x); otherwise the named Project form.
            if (fallback.Value == TruthValue.False && c.Operands[0] is NotExpression negated)
            {
                return new InspectionExpression(InspectionKind.IsFalse, this.Visit(negated.Operand));
            }

            return new ProjectExpression(this.Visit(c.Operands[0]), fallback.Value == TruthValue.True);
        }
    }
}
