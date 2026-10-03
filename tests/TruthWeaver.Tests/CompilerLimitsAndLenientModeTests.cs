namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>Ticket 09: compiler resource limits and <see cref="CompilationMode.Lenient"/>.</summary>
public sealed class CompilerLimitsAndLenientModeTests
{
    [Fact]
    public void Default_options_are_32_512_20_and_strict()
    {
        CompilerOptions defaults = CompilerOptions.Default;

        Assert.Equal(32, defaults.MaxDepth);
        Assert.Equal(512, defaults.MaxNodeCount);
        Assert.Equal(20, defaults.MaxAnalysisTerms);
        Assert.Equal(CompilationMode.Strict, defaults.Mode);
    }

    [Fact]
    public void Rule_exceeding_max_depth_is_a_compile_error()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry, new CompilerOptions(MaxDepth: 5));
        string deeplyNested = string.Concat(Enumerable.Repeat("NOT ", 10)) + "a";

        CompilationResult<RuleTestContext> result = compiler.Compile(deeplyNested);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MaxDepthExceeded);
    }

    [Fact]
    public void Rule_exceeding_max_node_count_is_a_compile_error()
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        for (int i = 0; i < 60; i++)
        {
            builder = builder.AddConstant($"p{i}", true);
        }

        RuleCompiler<RuleTestContext> compiler = new(builder.Build(), new CompilerOptions(MaxNodeCount: 10));
        string manyTerms = string.Join(" AND ", Enumerable.Range(0, 60).Select(i => $"p{i}"));

        CompilationResult<RuleTestContext> result = compiler.Compile(manyTerms);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MaxNodeCountExceeded);
    }

    [Fact]
    public void Rule_with_more_distinct_terms_than_analysis_cap_compiles_and_reports_info_skip()
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        for (int i = 0; i < 5; i++)
        {
            builder = builder.AddConstant($"p{i}", true);
        }

        RuleCompiler<RuleTestContext> compiler = new(builder.Build(), new CompilerOptions(MaxAnalysisTerms: 3));
        string rule = string.Join(" OR ", Enumerable.Range(0, 5).Select(i => $"p{i}"));

        CompilationResult<RuleTestContext> result = compiler.Compile(rule);

        Assert.True(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.AnalysisSkippedTooManyTerms, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
    }

    [Fact]
    public void Lenient_mode_compiles_an_unregistered_predicate_to_a_permanently_unknown_term()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Build(),
            new CompilerOptions(Mode: CompilationMode.Lenient)
        );

        CompilationResult<RuleTestContext> result = compiler.Compile("noSuchPredicate");

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task Lenient_mode_unknown_term_evaluates_to_unknown_at_runtime()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Build(),
            new CompilerOptions(Mode: CompilationMode.Lenient)
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("noSuchPredicate").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
    }

    [Fact]
    public void Lenient_mode_heterogeneous_array_literal_against_unregistered_predicate_compiles_without_error()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Build(),
            new CompilerOptions(Mode: CompilationMode.Lenient)
        );

        CompilationResult<RuleTestContext> result = compiler.Compile("noSuchPredicate(values: [1, \"two\", 3])");

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task Lenient_mode_heterogeneous_array_literal_term_still_evaluates_to_unknown_at_runtime()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Build(),
            new CompilerOptions(Mode: CompilationMode.Lenient)
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("noSuchPredicate(values: [1, \"two\", 3])").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
    }

    /// <summary>
    /// A node that fails validation is replaced by an Unknown constant (never False), so a failure
    /// can never look like a negative answer even to code that inspects the intermediate tree.
    /// </summary>
    [Fact]
    public void Placeholder_WhenSubstitutingAFailedNode_IsAnUnknownConstant_Test()
    {
        // Arrange / Act
        Ast.Expression placeholder = FailedNode.Placeholder;

        // Assert
        Ast.ConstantExpression constant = Assert.IsType<Ast.ConstantExpression>(placeholder);
        Assert.Equal(TruthValue.Unknown, constant.Value);
    }

    [Fact]
    public void Strict_mode_rejects_the_identical_rule_text_lenient_mode_accepts()
    {
        RuleCompiler<RuleTestContext> strictCompiler = new(PredicateRegistry<RuleTestContext>.CreateBuilder().Build());

        CompilationResult<RuleTestContext> result = strictCompiler.Compile("noSuchPredicate");

        Assert.False(result.Succeeded);
        Assert.Null(result.CompiledRule);
    }
}
