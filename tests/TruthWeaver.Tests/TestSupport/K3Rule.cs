namespace TruthWeaver.Tests.TestSupport;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;

/// <summary>
/// Compiles one rule text once, over zero-argument predicates named <c>a</c>, <c>b</c>, <c>c</c>…,
/// and evaluates it repeatedly under different K3 assignments of those predicates — the single
/// public-pipeline seam for conformance tests (rule text → <c>Compile</c> → <c>EvaluateAsync</c> → <see cref="Decision"/>).
/// </summary>
public sealed class K3Rule
{
    private readonly TruthValue[] current;

    private K3Rule(CompiledRule<RuleTestContext> compiled, TruthValue[] current)
    {
        this.Compiled = compiled;
        this.current = current;
    }

    /// <summary>Gets the compiled rule under test.</summary>
    public CompiledRule<RuleTestContext> Compiled { get; }

    /// <summary>Compiles <paramref name="ruleText"/> with <paramref name="arity"/> predicates named <c>a</c>, <c>b</c>, and so on.</summary>
    /// <param name="ruleText">The DSL text.</param>
    /// <param name="arity">The number of input predicates to register.</param>
    /// <returns>The compiled rule, or <see langword="null"/> when the text does not compile.</returns>
    public static K3Rule? TryCreate(string ruleText, int arity)
    {
        TruthValue[] current = new TruthValue[arity];
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        for (int i = 0; i < arity; i++)
        {
            int index = i;
            string name = ((char)('a' + i)).ToString();

            // Every input, including Unknown, is delivered directly as the predicate's answer.
            builder = builder.Add(
                PredicateSchema.NoArguments(name, name, $"Conformance input '{name}'."),
                (_, _, _) => ValueTask.FromResult(current[index])
            );
        }

        CompilationResult<RuleTestContext> result = new RuleCompiler<RuleTestContext>(builder.Build()).Compile(ruleText);
        return result.CompiledRule is { } rule ? new K3Rule(rule, current) : null;
    }

    /// <summary>
    /// Applies a rewrite (for example <c>ExpandToPrimitives</c>) to the compiled rule and returns a rule that shares this
    /// rule's inputs, so the original and the rewritten rule can be evaluated under the very same assignment.
    /// </summary>
    /// <param name="rewrite">The rewrite to apply.</param>
    /// <returns>The rewritten rule wired to the same input predicates.</returns>
    public K3Rule Rewrite(Func<CompiledRule<RuleTestContext>, CompiledRule<RuleTestContext>> rewrite)
    {
        return new K3Rule(rewrite(this.Compiled), this.current);
    }

    /// <summary>Evaluates the rule with the inputs set to <paramref name="assignment"/>.</summary>
    /// <param name="assignment">One K3 value per input predicate.</param>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>The evaluation result.</returns>
    public Task<Decision> EvaluateAsync(IReadOnlyList<TruthValue> assignment, CancellationToken cancellationToken)
    {
        for (int i = 0; i < assignment.Count; i++)
        {
            this.current[i] = assignment[i];
        }

        return this.Compiled.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: cancellationToken
        );
    }
}
