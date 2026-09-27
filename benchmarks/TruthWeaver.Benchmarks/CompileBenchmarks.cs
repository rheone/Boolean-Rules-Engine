namespace TruthWeaver.Benchmarks;

using BenchmarkDotNet.Attributes;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Registry;

/// <summary>
/// Measures <see cref="RuleCompiler{TContext}.CompileJson(string)"/>'s end-to-end cost — Parse →
/// Validate → Analyze → Build (ADR-0003), including the BDD-based tautology/contradiction analyzer
/// (<c>Analysis.Analyzer</c>) — across two representative rule sizes. <see cref="RuleSize.Large"/>
/// raises <see cref="CompilerOptions.MaxAnalysisTerms"/> so the analyzer actually runs at that size
/// instead of being skipped past the default cap of 20 distinct terms.
/// </summary>
[MemoryDiagnoser]
public class CompileBenchmarks
{
    private RuleCompiler<BenchmarkContext> compiler = null!;
    private string ruleJson = null!;

    /// <summary>Gets or sets the rule size this benchmark case compiles.</summary>
    [Params(RuleSize.Small, RuleSize.Large)]
    public RuleSize Size { get; set; }

    /// <summary>Builds the registry, compiler, and pre-rendered JSON text for <see cref="Size"/>, outside the measured operation.</summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        (int termCount, int groupSize) = this.Size switch
        {
            RuleSize.Small => (10, 2),
            RuleSize.Large => (200, 4),
            _ => throw new NotSupportedException($"Unhandled rule size '{this.Size}'."),
        };

        PredicateRegistry<BenchmarkContext> registry = RuleFixtures.BuildRegistry(termCount);
        CompilerOptions options = new(MaxAnalysisTerms: Math.Max(20, termCount));
        this.compiler = new RuleCompiler<BenchmarkContext>(registry, options);

        RuleBuilder rule = RuleFixtures.BuildGroupedRule(termCount, groupSize);
        this.ruleJson = rule.ToJson();
    }

    /// <summary>Compiles the pre-rendered rule JSON, exercising the full Parse/Validate/Analyze/Build pipeline.</summary>
    /// <returns>The compilation result, so the compiler can't be dead-code-eliminated.</returns>
    [Benchmark]
    public CompilationResult<BenchmarkContext> Compile()
    {
        return this.compiler.CompileJson(this.ruleJson);
    }
}
