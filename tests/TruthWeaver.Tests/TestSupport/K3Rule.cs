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
    private readonly CompiledRule<RuleTestContext> compiled;
    private readonly TruthValue[] current;

    private K3Rule(CompiledRule<RuleTestContext> compiled, TruthValue[] current)
    {
        this.compiled = compiled;
        this.current = current;
    }

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

            // An Unknown input is delivered as a thrown fault until predicates can return TruthValue
            // directly (k3-conformance ticket 02); the evaluator maps it to Unknown either way.
            builder = builder.Add(
                PredicateSchema.NoArguments(name, name, $"Conformance input '{name}'."),
                (_, _, _) =>
                    current[index] switch
                    {
                        TruthValue.True => ValueTask.FromResult(true),
                        TruthValue.False => ValueTask.FromResult(false),
                        _ => throw new InvalidOperationException($"'{name}' is Unknown."),
                    }
            );
        }

        CompilationResult<RuleTestContext> result = new RuleCompiler<RuleTestContext>(builder.Build()).Compile(ruleText);
        return result.CompiledRule is { } rule ? new K3Rule(rule, current) : null;
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

        return this.compiled.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: cancellationToken
        );
    }
}
