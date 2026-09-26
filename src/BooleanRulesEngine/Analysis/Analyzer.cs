namespace BooleanRulesEngine.Analysis;

using BooleanRulesEngine.Abstractions;
using BooleanRulesEngine.Ast;
using BooleanRulesEngine.Compilation;
using BooleanRulesEngine.Diagnostics;
using BooleanRulesEngine.Printing;

/// <summary>
/// The BDD-based analyzer step of the compilation pipeline (ADR-0003: Parse → Validate → Analyze →
/// Build): flags sub-expressions that are structurally always-true or always-false, using term
/// identity (CONTEXT.md) to recognize repeated references to the same variable — e.g.
/// <c>hasRole(role: "Y") AND NOT hasRole(role: "Y")</c> is a structural contradiction regardless of
/// what <c>hasRole</c> actually returns at evaluation time. These are <see cref="DiagnosticSeverity.Warning"/>
/// diagnostics — they never block compilation.
/// </summary>
internal static class Analyzer
{
    /// <summary>Analyzes a compiled tree for structural tautologies and contradictions.</summary>
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
        Build(root, bdd, variableIndex, diagnostics);
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
            case ExactlyOneExpression e:
                foreach (Expression o in e.Operands)
                {
                    CollectTerms(o, terms);
                }

                break;
            case AtLeastExpression al:
                foreach (Expression o in al.Operands)
                {
                    CollectTerms(o, terms);
                }

                break;
        }
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

    private static void Diagnose(int nodeId, Expression node, List<Diagnostic> diagnostics)
    {
        if (nodeId == BddManager.True)
        {
            diagnostics.Add(
                Diagnostic.Warning(
                    DiagnosticCodes.StructuralTautology,
                    $"This sub-expression is a structural tautology (always true): {CanonicalPrinter.Print(node)}",
                    SourceSpan.None
                )
            );
        }
        else if (nodeId == BddManager.False)
        {
            diagnostics.Add(
                Diagnostic.Warning(
                    DiagnosticCodes.StructuralContradiction,
                    $"This sub-expression is a structural contradiction (always false): {CanonicalPrinter.Print(node)}",
                    SourceSpan.None
                )
            );
        }
    }

    private static int Build(
        Expression node,
        BddManager bdd,
        Dictionary<TermIdentity, int> variableIndex,
        List<Diagnostic> diagnostics
    )
    {
        int nodeId;
        switch (node)
        {
            case ConstantExpression c:
                return c.Value ? BddManager.True : BddManager.False;
            case TermExpression t:
                if (!variableIndex.TryGetValue(t.Identity, out int index))
                {
                    index = variableIndex.Count;
                    variableIndex[t.Identity] = index;
                }

                return bdd.Variable(index);
            case NotExpression n:
                nodeId = bdd.Not(Build(n.Operand, bdd, variableIndex, diagnostics));
                break;
            case AndExpression a:
                nodeId = BddManager.True;
                foreach (Expression operand in a.Operands)
                {
                    nodeId = bdd.And(nodeId, Build(operand, bdd, variableIndex, diagnostics));
                }

                break;
            case OrExpression o:
                nodeId = BddManager.False;
                foreach (Expression operand in o.Operands)
                {
                    nodeId = bdd.Or(nodeId, Build(operand, bdd, variableIndex, diagnostics));
                }

                break;
            case XorExpression x:
                nodeId = bdd.Xor(
                    Build(x.Left, bdd, variableIndex, diagnostics),
                    Build(x.Right, bdd, variableIndex, diagnostics)
                );
                break;
            case ExactlyOneExpression e:
                List<int> exactlyOneOperandIds = [.. e.Operands.Select(o => Build(o, bdd, variableIndex, diagnostics))];
                nodeId = bdd.And(
                    AtLeastBdd(bdd, exactlyOneOperandIds, 1, 0),
                    bdd.Not(AtLeastBdd(bdd, exactlyOneOperandIds, 2, 0))
                );
                break;
            case AtLeastExpression al:
                List<int> atLeastOperandIds = [.. al.Operands.Select(o => Build(o, bdd, variableIndex, diagnostics))];
                nodeId = AtLeastBdd(bdd, atLeastOperandIds, al.K, 0);
                break;
            default:
                throw new InvalidOperationException($"Unhandled expression type '{node.GetType()}'.");
        }

        Diagnose(nodeId, node, diagnostics);
        return nodeId;
    }
}
