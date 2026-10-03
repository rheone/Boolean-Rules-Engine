namespace TruthWeaver.Rewriting;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;

/// <summary>
/// Structural helpers shared by the opt-in rewrites (compression, canonicalisation and simplification): generic child
/// mapping, tree size and operand-array construction. Nothing here changes what an expression means.
/// </summary>
internal static class ExpressionTools
{
    /// <summary>Builds an operand array from <paramref name="operands"/>.</summary>
    /// <param name="operands">The operands, in order.</param>
    /// <returns>The immutable operand array.</returns>
    public static EquatableArray<Expression> Array(IEnumerable<Expression> operands)
    {
        return new EquatableArray<Expression>(operands);
    }

    /// <summary>
    /// Returns <paramref name="node"/> with every direct child replaced by <paramref name="map"/> of that child. When no
    /// child changes (by reference) the original node is returned, which keeps shared sub-trees shared and lets callers
    /// detect "nothing happened" cheaply.
    /// </summary>
    /// <param name="node">The node whose children are mapped.</param>
    /// <param name="map">The child transformation.</param>
    /// <returns>The node with mapped children, or <paramref name="node"/> itself when none changed.</returns>
    public static Expression MapChildren(Expression node, Func<Expression, Expression> map)
    {
        return node switch
        {
            ConstantExpression or TermExpression => node,
            NotExpression n => Remap(n, map(n.Operand), static (_, x) => new NotExpression(x), n.Operand),
            AndExpression a => MapList(a, a.Operands, map, static (_, ops) => new AndExpression(ops)),
            OrExpression o => MapList(o, o.Operands, map, static (_, ops) => new OrExpression(ops)),
            XorExpression x => MapPair(x, x.Left, x.Right, map, static (l, r) => new XorExpression(l, r)),
            EquivalentExpression e => MapPair(e, e.Left, e.Right, map, static (l, r) => new EquivalentExpression(l, r)),
            ImpliesExpression i => MapPair(i, i.Antecedent, i.Consequent, map, static (l, r) => new ImpliesExpression(l, r)),
            NandExpression nd => MapPair(nd, nd.Left, nd.Right, map, static (l, r) => new NandExpression(l, r)),
            NorExpression nr => MapPair(nr, nr.Left, nr.Right, map, static (l, r) => new NorExpression(l, r)),
            NxorExpression nx => MapList(nx, nx.Operands, map, static (_, ops) => new NxorExpression(ops)),
            AnyExpression any => MapList(any, any.Operands, map, static (_, ops) => new AnyExpression(ops)),
            AllExpression all => MapList(all, all.Operands, map, static (_, ops) => new AllExpression(ops)),
            NoneExpression none => MapList(none, none.Operands, map, static (_, ops) => new NoneExpression(ops)),
            ExactlyOneExpression one => MapList(one, one.Operands, map, static (_, ops) => new ExactlyOneExpression(ops)),
            CoalesceExpression c => MapList(c, c.Operands, map, static (_, ops) => new CoalesceExpression(ops)),
            ThresholdExpression t => MapList(
                t,
                t.Operands,
                map,
                static (original, ops) =>
                    new ThresholdExpression(((ThresholdExpression)original).Comparison, ((ThresholdExpression)original).K, ops)
            ),
            BetweenExpression b => MapList(
                b,
                b.Operands,
                map,
                static (original, ops) =>
                    new BetweenExpression(((BetweenExpression)original).Min, ((BetweenExpression)original).Max, ops)
            ),
            InspectionExpression s => Remap(
                s,
                map(s.Operand),
                static (o, x) => new InspectionExpression(((InspectionExpression)o).Kind, x),
                s.Operand
            ),
            ProjectExpression p => Remap(
                p,
                map(p.Operand),
                static (o, x) => new ProjectExpression(x, ((ProjectExpression)o).UnknownAs),
                p.Operand
            ),
            IfExpression f => MapIf(f, map),
            _ => throw new InvalidOperationException($"Unhandled expression type '{node.GetType().Name}'."),
        };
    }

    /// <summary>Lists the direct children of <paramref name="node"/> in operand order.</summary>
    /// <param name="node">The node.</param>
    /// <returns>Its operands (empty for a constant or term).</returns>
    public static List<Expression> Children(Expression node)
    {
        List<Expression> children = [];
        MapChildren(
            node,
            child =>
            {
                children.Add(child);
                return child;
            }
        );
        return children;
    }

    /// <summary>
    /// The number of nodes in <paramref name="root"/> counted as a tree: a sub-expression shared in memory but appearing
    /// twice in the printed rule counts twice, because the printed (recompilable) size is what the node limit measures.
    /// </summary>
    /// <param name="root">The tree to measure.</param>
    /// <returns>The node count.</returns>
    public static long Size(Expression root)
    {
        return Size(root, new Dictionary<Expression, long>(ReferenceEqualityComparer.Instance));
    }

    private static long Size(Expression node, Dictionary<Expression, long> memo)
    {
        if (memo.TryGetValue(node, out long known))
        {
            return known;
        }

        // MapChildren is reused as a child enumerator so a new node type only needs one traversal to be added.
        long total = 1;
        MapChildren(
            node,
            child =>
            {
                total += Size(child, memo);
                return child;
            }
        );
        memo[node] = total;
        return total;
    }

    private static Expression Remap(
        Expression original,
        Expression mapped,
        Func<Expression, Expression, Expression> build,
        Expression previous
    )
    {
        return ReferenceEquals(mapped, previous) ? original : build(original, mapped);
    }

    private static Expression MapPair(
        Expression original,
        Expression left,
        Expression right,
        Func<Expression, Expression> map,
        Func<Expression, Expression, Expression> build
    )
    {
        Expression newLeft = map(left);
        Expression newRight = map(right);
        return ReferenceEquals(newLeft, left) && ReferenceEquals(newRight, right) ? original : build(newLeft, newRight);
    }

    private static Expression MapIf(IfExpression f, Func<Expression, Expression> map)
    {
        Expression condition = map(f.Condition);
        Expression whenTrue = map(f.WhenTrue);
        Expression whenFalse = map(f.WhenFalse);
        return
            ReferenceEquals(condition, f.Condition)
            && ReferenceEquals(whenTrue, f.WhenTrue)
            && ReferenceEquals(whenFalse, f.WhenFalse)
            ? f
            : new IfExpression(condition, whenTrue, whenFalse);
    }

    private static Expression MapList(
        Expression original,
        EquatableArray<Expression> operands,
        Func<Expression, Expression> map,
        Func<Expression, EquatableArray<Expression>, Expression> build
    )
    {
        Expression[] mapped = new Expression[operands.Count];
        bool changed = false;
        for (int i = 0; i < mapped.Length; i++)
        {
            mapped[i] = map(operands[i]);
            changed |= !ReferenceEquals(mapped[i], operands[i]);
        }

        return changed ? build(original, new EquatableArray<Expression>(mapped)) : original;
    }
}
