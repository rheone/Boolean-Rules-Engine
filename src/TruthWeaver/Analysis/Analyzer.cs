namespace TruthWeaver.Analysis;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Printing;

/// <summary>
/// The BDD-based analyzer step of the compilation pipeline (ADR-0003: Parse → Validate → Analyze →
/// Build). It reasons in Strong K3 (ADR-0005 decision 17) with a dual-rail BDD: every sub-expression is
/// represented by two BDDs, <em>definitely true</em> (it is <c>True</c>) and <em>possibly true</em>
/// (it is <c>True</c> or <c>Unknown</c>, i.e. not <c>False</c>). A sub-expression is reported as a
/// tautology only when it is <c>True</c> for every <c>{True, False, Unknown}</c> assignment of its terms
/// (the definitely-true rail is constant true), and as a contradiction only when it is <c>False</c> for
/// every assignment (the possibly-true rail is constant false). So <c>A AND NOT A</c> and
/// <c>A OR NOT A</c> are not reported: both are <c>Unknown</c> when <c>A</c> is. Term identity
/// (CONTEXT.md) recognises repeated references to the same variable. Findings are
/// <see cref="DiagnosticSeverity.Warning"/> diagnostics — they never block compilation.
/// </summary>
internal static class Analyzer
{
    private static readonly DualRail TrueRail = new(BddManager.True, BddManager.True);
    private static readonly DualRail FalseRail = new(BddManager.False, BddManager.False);
    private static readonly DualRail UnknownRail = new(BddManager.False, BddManager.True);

    /// <summary>Analyzes a compiled tree for Strong K3 tautologies and contradictions.</summary>
    /// <param name="root">The compiled expression tree.</param>
    /// <param name="options">The compiler options, whose <c>MaxAnalysisTerms</c> caps this analysis.</param>
    /// <returns>The diagnostics raised (never <see cref="DiagnosticSeverity.Error"/>).</returns>
    public static IReadOnlyList<Diagnostic> Analyze(Expression root, CompilerOptions options)
    {
        HashSet<TermIdentity> distinctTerms = [];
        CollectTerms(root, distinctTerms);
        if (distinctTerms.Count > options.MaxAnalysisTerms)
        {
            return
            [
                Diagnostic.Info(
                    DiagnosticCodes.AnalysisSkippedTooManyTerms,
                    $"Constant/contradiction analysis skipped: {distinctTerms.Count} distinct terms exceeds the configured cap of {options.MaxAnalysisTerms}.",
                    SourceSpan.None
                ),
            ];
        }

        Dictionary<TermIdentity, int> variableIndex = [];
        BddManager bdd = new();
        List<Diagnostic> diagnostics = [];
        _ = Build(root, bdd, variableIndex, diagnostics);
        return diagnostics;
    }

    private static void CollectTerms(Expression node, HashSet<TermIdentity> terms)
    {
        switch (node)
        {
            case TermExpression t:
                terms.Add(t.Identity);
                break;
            case NotExpression n:
                CollectTerms(n.Operand, terms);
                break;
            case AndExpression a:
                foreach (Expression o in a.Operands)
                {
                    CollectTerms(o, terms);
                }

                break;
            case OrExpression o2:
                foreach (Expression o in o2.Operands)
                {
                    CollectTerms(o, terms);
                }

                break;
            case XorExpression x:
                CollectTerms(x.Left, terms);
                CollectTerms(x.Right, terms);
                break;
            case EquivalentExpression xn:
                CollectTerms(xn.Left, terms);
                CollectTerms(xn.Right, terms);
                break;
            case NandExpression nd:
                CollectTerms(nd.Left, terms);
                CollectTerms(nd.Right, terms);
                break;
            case NorExpression nr:
                CollectTerms(nr.Left, terms);
                CollectTerms(nr.Right, terms);
                break;
            case ImpliesExpression im:
                CollectTerms(im.Antecedent, terms);
                CollectTerms(im.Consequent, terms);
                break;
            case NxorExpression nx:
                foreach (Expression o in nx.Operands)
                {
                    CollectTerms(o, terms);
                }

                break;
            case AnyExpression an:
                foreach (Expression o in an.Operands)
                {
                    CollectTerms(o, terms);
                }

                break;
            case AllExpression al:
                foreach (Expression o in al.Operands)
                {
                    CollectTerms(o, terms);
                }

                break;
            case NoneExpression no:
                foreach (Expression o in no.Operands)
                {
                    CollectTerms(o, terms);
                }

                break;
            case ExactlyOneExpression e:
                foreach (Expression o in e.Operands)
                {
                    CollectTerms(o, terms);
                }

                break;
            case ThresholdExpression th:
                foreach (Expression o in th.Operands)
                {
                    CollectTerms(o, terms);
                }

                break;
            case BetweenExpression bt:
                foreach (Expression o in bt.Operands)
                {
                    CollectTerms(o, terms);
                }

                break;
            case CoalesceExpression co:
                foreach (Expression o in co.Operands)
                {
                    CollectTerms(o, terms);
                }

                break;
            case InspectionExpression ins:
                CollectTerms(ins.Operand, terms);
                break;
            case IfExpression iff:
                CollectTerms(iff.Condition, terms);
                CollectTerms(iff.WhenTrue, terms);
                CollectTerms(iff.WhenFalse, terms);
                break;
        }
    }

    /// <summary>
    /// Strong K3 negation on the rails: <c>NOT x</c> is definitely true when <c>x</c> is not even possibly
    /// true, and possibly true when <c>x</c> is not definitely true. <c>Unknown</c> stays <c>Unknown</c>.
    /// </summary>
    private static DualRail Not(BddManager bdd, DualRail x)
    {
        return new DualRail(bdd.Not(x.Possible), bdd.Not(x.Definite));
    }

    /// <summary>Strong K3 conjunction: definitely true needs both definitely true; possibly true needs both possibly true.</summary>
    private static DualRail And(BddManager bdd, DualRail x, DualRail y)
    {
        return new DualRail(bdd.And(x.Definite, y.Definite), bdd.And(x.Possible, y.Possible));
    }

    /// <summary>Strong K3 disjunction: the dual of <see cref="And"/>.</summary>
    private static DualRail Or(BddManager bdd, DualRail x, DualRail y)
    {
        return new DualRail(bdd.Or(x.Definite, y.Definite), bdd.Or(x.Possible, y.Possible));
    }

    /// <summary>NAND as its primitive definition <c>NOT (x AND y)</c>.</summary>
    private static DualRail Nand(BddManager bdd, DualRail x, DualRail y)
    {
        return Not(bdd, And(bdd, x, y));
    }

    /// <summary>NOR as its primitive definition <c>NOT (x OR y)</c>.</summary>
    private static DualRail Nor(BddManager bdd, DualRail x, DualRail y)
    {
        return Not(bdd, Or(bdd, x, y));
    }

    /// <summary>Material implication as its primitive definition <c>NOT x OR y</c>.</summary>
    private static DualRail Implies(BddManager bdd, DualRail x, DualRail y)
    {
        return Or(bdd, Not(bdd, x), y);
    }

    /// <summary>
    /// Binary XOR as <c>(x AND NOT y) OR (NOT x AND y)</c> — the primitive definition, so it is
    /// <c>Unknown</c> whenever either side is.
    /// </summary>
    private static DualRail Xor(BddManager bdd, DualRail x, DualRail y)
    {
        return Or(bdd, And(bdd, x, Not(bdd, y)), And(bdd, Not(bdd, x), y));
    }

    /// <summary>
    /// N-ary parity as the left fold of binary <see cref="Xor"/>. Since XOR is <c>Unknown</c> whenever either side
    /// is, the fold is <c>Unknown</c> whenever any operand is, and otherwise true for an odd number of true operands.
    /// </summary>
    private static DualRail Nxor(BddManager bdd, IReadOnlyList<DualRail> operands)
    {
        DualRail result = operands[0];
        for (int i = 1; i < operands.Count; i++)
        {
            result = Xor(bdd, result, operands[i]);
        }

        return result;
    }

    /// <summary>
    /// <c>COALESCE</c> as a right fold of the binary form. For <c>x</c> with rail <c>(D, P)</c>, <c>x</c> is
    /// <c>True</c> iff <c>D</c>, <c>False</c> iff <c>NOT P</c> and <c>Unknown</c> iff <c>P AND NOT D</c>; the result is
    /// <c>x</c> when known and <c>y</c> when <c>x</c> is Unknown. So it is definitely true when
    /// <c>D_x OR (Unknown_x AND D_y)</c>, which simplifies to <c>D_x OR (P_x AND D_y)</c>, and possibly true when it is
    /// not definitely false: <c>NOT (NOT P_x OR (Unknown_x AND NOT P_y))</c>, i.e. <c>P_x AND (D_x OR P_y)</c>.
    /// </summary>
    private static DualRail Coalesce(BddManager bdd, IReadOnlyList<DualRail> operands)
    {
        DualRail result = operands[^1];
        for (int i = operands.Count - 2; i >= 0; i--)
        {
            DualRail x = operands[i];
            result = new DualRail(
                bdd.Or(x.Definite, bdd.And(x.Possible, result.Definite)),
                bdd.And(x.Possible, bdd.Or(x.Definite, result.Possible))
            );
        }

        return result;
    }

    /// <summary>
    /// An inspection on the rails. The tested state is a definite fact about <c>x</c>, so both result rails are the same
    /// BDD: <c>IsTrue</c> is <c>D</c>, <c>IsFalse</c> is <c>NOT P</c>, <c>IsUnknown</c> is <c>P AND NOT D</c> and
    /// <c>IsKnown</c> is <c>D OR NOT P</c> (for <c>x</c> with rail <c>(D, P)</c>); the result is never <c>Unknown</c>.
    /// </summary>
    private static DualRail Inspect(BddManager bdd, InspectionKind kind, DualRail x)
    {
        int isTrue = kind switch
        {
            InspectionKind.IsTrue => x.Definite,
            InspectionKind.IsFalse => bdd.Not(x.Possible),
            InspectionKind.IsUnknown => bdd.And(x.Possible, bdd.Not(x.Definite)),
            InspectionKind.IsKnown => bdd.Or(x.Definite, bdd.Not(x.Possible)),
            _ => throw new InvalidOperationException($"Unhandled inspection kind '{kind}'."),
        };
        return new DualRail(isTrue, isTrue);
    }

    /// <summary>
    /// <c>If(c, t, f)</c> as its primitive definition <c>(c AND t) OR (NOT c AND f) OR (t AND f)</c>. The last, consensus
    /// term is what stops an Unknown condition from guessing: with it <c>If(Unknown, True, True)</c> is <c>True</c>, and
    /// for a definite condition it never changes the multiplexer's value.
    /// </summary>
    private static DualRail If(BddManager bdd, DualRail condition, DualRail whenTrue, DualRail whenFalse)
    {
        return Or(
            bdd,
            Or(bdd, And(bdd, condition, whenTrue), And(bdd, Not(bdd, condition), whenFalse)),
            And(bdd, whenTrue, whenFalse)
        );
    }

    /// <summary>
    /// "At least <paramref name="k"/> operands are true" over the interval semantics: definitely true when
    /// the definitely-true operands alone reach <paramref name="k"/>, possibly true when the possibly-true
    /// operands can. Every other cardinality operator is built from this and <see cref="Not"/>.
    /// </summary>
    private static DualRail AtLeast(BddManager bdd, IReadOnlyList<DualRail> operands, int k)
    {
        return new DualRail(
            AtLeastBdd(bdd, [.. operands.Select(o => o.Definite)], k, 0),
            AtLeastBdd(bdd, [.. operands.Select(o => o.Possible)], k, 0)
        );
    }

    private static int AtLeastBdd(BddManager bdd, IReadOnlyList<int> operandIds, int k, int fromIndex)
    {
        int remaining = operandIds.Count - fromIndex;
        if (k <= 0)
        {
            return BddManager.True;
        }

        if (k > remaining)
        {
            return BddManager.False;
        }

        int withFirstTrue = bdd.And(operandIds[fromIndex], AtLeastBdd(bdd, operandIds, k - 1, fromIndex + 1));
        int withoutFirst = bdd.And(bdd.Not(operandIds[fromIndex]), AtLeastBdd(bdd, operandIds, k, fromIndex + 1));
        return bdd.Or(withFirstTrue, withoutFirst);
    }

    /// <summary>"Exactly <paramref name="k"/> operands are true": <c>AtLeast(k) AND NOT AtLeast(k + 1)</c>.</summary>
    private static DualRail Exactly(BddManager bdd, IReadOnlyList<DualRail> operands, int k)
    {
        return And(bdd, AtLeast(bdd, operands, k), Not(bdd, AtLeast(bdd, operands, k + 1)));
    }

    private static string Message(string alwaysValue, Expression node)
    {
        return $"Strong K3 analysis: this sub-expression is {alwaysValue} for every True/False/Unknown assignment of its terms: {CanonicalPrinter.Print(node)}";
    }

    private static void Diagnose(DualRail rail, Expression node, List<Diagnostic> diagnostics)
    {
        if (rail.Definite == BddManager.True)
        {
            // Definitely true in every assignment: it can never be False or Unknown.
            diagnostics.Add(Diagnostic.Warning(DiagnosticCodes.StructuralTautology, Message("True", node), SourceSpan.None));
        }
        else if (rail.Possible == BddManager.False)
        {
            // Never possibly true in any assignment: it can never be True or Unknown.
            diagnostics.Add(
                Diagnostic.Warning(DiagnosticCodes.StructuralContradiction, Message("False", node), SourceSpan.None)
            );
        }
    }

    private static DualRail Build(
        Expression node,
        BddManager bdd,
        Dictionary<TermIdentity, int> variableIndex,
        List<Diagnostic> diagnostics
    )
    {
        DualRail rail;
        switch (node)
        {
            case ConstantExpression c:
                return c.Value switch
                {
                    TruthValue.True => TrueRail,
                    TruthValue.False => FalseRail,
                    _ => UnknownRail,
                };
            case TermExpression t:
                if (!variableIndex.TryGetValue(t.Identity, out int index))
                {
                    index = variableIndex.Count;
                    variableIndex[t.Identity] = index;
                }

                // Each term gets two independent BDD variables: d ("is True") and q ("is Unknown").
                // The rails are d and d OR q, so every variable setting is a valid K3 state
                // (d=1 is True, d=0 and q=1 is Unknown, d=0 and q=0 is False) and a "definitely true but
                // not possibly true" state can never be produced.
                int definite = bdd.Variable(2 * index);
                int unknown = bdd.Variable((2 * index) + 1);
                return new DualRail(definite, bdd.Or(definite, unknown));
            case NotExpression n:
                rail = Not(bdd, Build(n.Operand, bdd, variableIndex, diagnostics));
                break;
            case AndExpression a:
                rail = TrueRail;
                foreach (Expression operand in a.Operands)
                {
                    rail = And(bdd, rail, Build(operand, bdd, variableIndex, diagnostics));
                }

                break;
            case OrExpression o:
                rail = FalseRail;
                foreach (Expression operand in o.Operands)
                {
                    rail = Or(bdd, rail, Build(operand, bdd, variableIndex, diagnostics));
                }

                break;
            case XorExpression x:
                rail = Xor(
                    bdd,
                    Build(x.Left, bdd, variableIndex, diagnostics),
                    Build(x.Right, bdd, variableIndex, diagnostics)
                );
                break;
            case EquivalentExpression xn:
                rail = Not(
                    bdd,
                    Xor(bdd, Build(xn.Left, bdd, variableIndex, diagnostics), Build(xn.Right, bdd, variableIndex, diagnostics))
                );
                break;
            case NandExpression nd:
                rail = Nand(
                    bdd,
                    Build(nd.Left, bdd, variableIndex, diagnostics),
                    Build(nd.Right, bdd, variableIndex, diagnostics)
                );
                break;
            case NorExpression nr:
                rail = Nor(
                    bdd,
                    Build(nr.Left, bdd, variableIndex, diagnostics),
                    Build(nr.Right, bdd, variableIndex, diagnostics)
                );
                break;
            case ImpliesExpression im:
                rail = Implies(
                    bdd,
                    Build(im.Antecedent, bdd, variableIndex, diagnostics),
                    Build(im.Consequent, bdd, variableIndex, diagnostics)
                );
                break;
            case NxorExpression nx:
                rail = Nxor(bdd, BuildOperands(nx.Operands, bdd, variableIndex, diagnostics));
                break;
            case AnyExpression an:
                rail = AtLeast(bdd, BuildOperands(an.Operands, bdd, variableIndex, diagnostics), 1);
                break;
            case AllExpression al:
                List<DualRail> allOperands = BuildOperands(al.Operands, bdd, variableIndex, diagnostics);
                rail = AtLeast(bdd, allOperands, allOperands.Count);
                break;
            case NoneExpression no:
                // AtMost(0, ...) is "not even one operand is true": the negation of AtLeast(1, ...).
                rail = Not(bdd, AtLeast(bdd, BuildOperands(no.Operands, bdd, variableIndex, diagnostics), 1));
                break;
            case ExactlyOneExpression e:
                rail = Exactly(bdd, BuildOperands(e.Operands, bdd, variableIndex, diagnostics), 1);
                break;
            case BetweenExpression bt:
                // AND(AtLeast(min, ...), AtMost(max, ...)): AtMost(max) is the negation of AtLeast(max + 1).
                List<DualRail> betweenOperands = BuildOperands(bt.Operands, bdd, variableIndex, diagnostics);
                rail = And(bdd, AtLeast(bdd, betweenOperands, bt.Min), Not(bdd, AtLeast(bdd, betweenOperands, bt.Max + 1)));
                break;
            case CoalesceExpression co:
                rail = Coalesce(bdd, BuildOperands(co.Operands, bdd, variableIndex, diagnostics));
                break;
            case InspectionExpression ins:
                rail = Inspect(bdd, ins.Kind, Build(ins.Operand, bdd, variableIndex, diagnostics));
                break;
            case IfExpression iff:
                rail = If(
                    bdd,
                    Build(iff.Condition, bdd, variableIndex, diagnostics),
                    Build(iff.WhenTrue, bdd, variableIndex, diagnostics),
                    Build(iff.WhenFalse, bdd, variableIndex, diagnostics)
                );
                break;
            case ThresholdExpression th:
                List<DualRail> operands = BuildOperands(th.Operands, bdd, variableIndex, diagnostics);
                rail = th.Comparison switch
                {
                    ThresholdComparison.AtLeast => AtLeast(bdd, operands, th.K),
                    ThresholdComparison.AtMost => Not(bdd, AtLeast(bdd, operands, th.K + 1)),
                    ThresholdComparison.GreaterThan => AtLeast(bdd, operands, th.K + 1),
                    ThresholdComparison.LessThan => Not(bdd, AtLeast(bdd, operands, th.K)),
                    ThresholdComparison.Exactly => Exactly(bdd, operands, th.K),
                    _ => throw new InvalidOperationException($"Unhandled threshold comparison '{th.Comparison}'."),
                };
                break;
            default:
                throw new InvalidOperationException($"Unhandled expression type '{node.GetType()}'.");
        }

        Diagnose(rail, node, diagnostics);
        return rail;
    }

    private static List<DualRail> BuildOperands(
        IEnumerable<Expression> operands,
        BddManager bdd,
        Dictionary<TermIdentity, int> variableIndex,
        List<Diagnostic> diagnostics
    )
    {
        return [.. operands.Select(o => Build(o, bdd, variableIndex, diagnostics))];
    }

    /// <summary>
    /// The two BDDs that describe one K3 value: <c>Definite</c> is true exactly when the value is
    /// <c>True</c>, <c>Possible</c> exactly when it is <c>True</c> or <c>Unknown</c>. The pair
    /// (<c>Definite</c>, <c>Possible</c>) encodes <c>True</c> = (1,1), <c>Unknown</c> = (0,1) and
    /// <c>False</c> = (0,0).
    /// </summary>
    /// <param name="Definite">The BDD node for "is definitely True".</param>
    /// <param name="Possible">The BDD node for "is True or Unknown" (not False).</param>
    private readonly record struct DualRail(int Definite, int Possible);
}
