namespace TruthWeaver.Benchmarks;

using TruthWeaver.Compilation;

/// <summary>The two representative rule sizes <see cref="CompileBenchmarks"/> and other benchmarks compile.</summary>
public enum RuleSize
{
    /// <summary>A small rule: 10 distinct terms in 5 two-term <c>OR</c> groups, well under every default compiler limit.</summary>
    Small,

    /// <summary>
    /// A large rule: 200 distinct terms in 50 four-term <c>OR</c> groups (~250 nodes) - still under the
    /// default node-count/depth limits, but well past the default 20-term analysis cap, so
    /// <see cref="CompileBenchmarks"/> raises <see cref="CompilerOptions.MaxAnalysisTerms"/> to let the
    /// BDD analyzer actually run across the whole tree.
    /// </summary>
    Large,
}
