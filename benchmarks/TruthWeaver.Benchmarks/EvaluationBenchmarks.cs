namespace TruthWeaver.Benchmarks;

using BenchmarkDotNet.Attributes;
using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;

/// <summary>
/// Measures <see cref="CompiledRule{TContext}.EvaluateAsync"/>'s eval-time memoized term-lookup cost
/// (ADR-0002) against a rule shaped as an <c>OR</c> of <see cref="BranchCount"/> <c>AND</c> branches
/// that all reference the same shared term alongside one branch-unique term
/// (<see cref="RuleFixtures.BuildSharedTermFanOut"/>). <see cref="EvaluationMode.Exhaustive"/> forces
/// every branch to actually run (rather than short-circuiting on the first <see langword="true"/>
/// branch), so the benchmark measures the full fan-out: one real predicate invocation for the shared
/// term (memoized after the first branch) plus one real invocation per branch-unique term.
/// </summary>
[MemoryDiagnoser]
public class EvaluationBenchmarks
{
    private const string SharedTermName = "sharedTerm";

    private static readonly EvaluationOptions ExhaustiveOptions = new(Mode: EvaluationMode.Exhaustive);

    private BenchmarkContext context = null!;
    private CompiledRule<BenchmarkContext> rule = null!;

    /// <summary>Gets or sets how many branches share the same term.</summary>
    [Params(10, 50, 200)]
    public int BranchCount { get; set; }

    /// <summary>Compiles the shared-term fan-out rule for <see cref="BranchCount"/>, outside the measured operation.</summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        PredicateRegistry<BenchmarkContext> registry = RuleFixtures.BuildSharedTermRegistry(SharedTermName, this.BranchCount);

        // Each branch contributes one AND node plus two term nodes, plus the root OR node - the default
        // MaxNodeCount (512) is too small once BranchCount grows past ~170.
        CompilerOptions options = new(MaxNodeCount: Math.Max(CompilerOptions.Default.MaxNodeCount, (this.BranchCount * 3) + 1));
        RuleCompiler<BenchmarkContext> compiler = new(registry, options);
        RuleBuilder builder = RuleFixtures.BuildSharedTermFanOut(SharedTermName, this.BranchCount);
        CompilationResult<BenchmarkContext> result = builder.Compile(compiler);
        this.rule =
            result.CompiledRule
            ?? throw new InvalidOperationException("The shared-term fan-out benchmark rule failed to compile.");
        this.context = new BenchmarkContext();
    }

    /// <summary>Evaluates the shared-term fan-out rule, exercising per-evaluation memoized term lookup across every branch.</summary>
    /// <returns>The evaluation's decision, so the evaluator can't be dead-code-eliminated.</returns>
    [Benchmark]
    public Task<Decision> EvaluateAsync()
    {
        return this.rule.EvaluateAsync(this.context, NullServiceProvider.Instance, ExhaustiveOptions);
    }
}
