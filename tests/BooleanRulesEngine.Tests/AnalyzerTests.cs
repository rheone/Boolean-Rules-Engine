namespace BooleanRulesEngine.Tests;

using BooleanRulesEngine.Compilation;
using BooleanRulesEngine.Diagnostics;
using BooleanRulesEngine.Registry;
using BooleanRulesEngine.Tests.TestSupport;

/// <summary>Ticket 10: BDD-based analyzer — constant/contradiction diagnostics.</summary>
public sealed class AnalyzerTests
{
    [Fact]
    public void Contradiction_using_the_same_term_twice_is_flagged()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("hasRole(role: \"Y\") AND NOT hasRole(role: \"Y\")");

        Assert.True(result.Succeeded);
        Assert.Contains(
            result.Diagnostics,
            d => d.Code == DiagnosticCodes.StructuralContradiction && d.Severity == DiagnosticSeverity.Warning
        );
    }

    [Fact]
    public void Tautology_using_the_same_term_twice_is_flagged()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("hasRole(role: \"Y\") OR NOT hasRole(role: \"Y\")");

        Assert.True(result.Succeeded);
        Assert.Contains(
            result.Diagnostics,
            d => d.Code == DiagnosticCodes.StructuralTautology && d.Severity == DiagnosticSeverity.Warning
        );
    }

    [Fact]
    public void Rule_with_no_constant_or_contradictory_subexpression_produces_no_analysis_diagnostics()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("a AND (b OR c)");

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Theory]
    [InlineData("XOR")]
    [InlineData("ExactlyOne")]
    [InlineData("AtLeast")]
    [InlineData("AtMost")]
    [InlineData("GreaterThan")]
    [InlineData("LessThan")]
    [InlineData("Exactly")]
    public void Analysis_covers_every_operator_not_just_and_or_not(string variant)
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        string rule = variant switch
        {
            "XOR" => "a XOR NOT a",
            "ExactlyOne" => "ExactlyOne(a, NOT a)",
            "AtLeast" => "AtLeast(1, a, NOT a)",
            "AtMost" => "AtMost(1, a, NOT a)",
            "GreaterThan" => "GreaterThan(0, a, NOT a)",
            "LessThan" => "LessThan(2, a, NOT a)",
            _ => "Exactly(1, a, NOT a)",
        };

        CompilationResult<RuleTestContext> result = compiler.Compile(rule);

        Assert.True(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.StructuralTautology);
    }

    [Fact]
    public void Xnor_of_the_same_term_twice_is_flagged_as_a_tautology()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("a XNOR a");

        Assert.True(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.StructuralTautology);
    }

    [Fact]
    public void Xnor_of_a_term_and_its_negation_is_flagged_as_a_contradiction()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("a XNOR NOT a");

        Assert.True(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.StructuralContradiction);
    }

    [Fact]
    public void Diagnostics_are_warning_or_info_never_error()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("hasRole(role: \"Y\") AND NOT hasRole(role: \"Y\")");

        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Term_count_at_or_under_the_analysis_cap_runs_analysis_normally()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompilerWithMaxAnalysisTerms(2);

        CompilationResult<RuleTestContext> result = compiler.Compile("a AND NOT a");

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, d => d.Code == DiagnosticCodes.AnalysisSkippedTooManyTerms);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.StructuralContradiction);
    }

    [Fact]
    public void Term_count_exceeding_the_analysis_cap_skips_analysis_and_suppresses_the_contradiction()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompilerWithMaxAnalysisTerms(1);

        CompilationResult<RuleTestContext> result = compiler.Compile("a AND NOT a AND b");

        Assert.True(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.AnalysisSkippedTooManyTerms, diagnostic.Code);
        Assert.DoesNotContain(result.Diagnostics, d => d.Code == DiagnosticCodes.StructuralContradiction);
    }

    [Fact]
    public void Term_count_exactly_equal_to_the_analysis_cap_does_not_trigger_the_skip()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompilerWithMaxAnalysisTerms(1);

        CompilationResult<RuleTestContext> result = compiler.Compile("a AND a");

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(result.Diagnostics, d => d.Code == DiagnosticCodes.AnalysisSkippedTooManyTerms);
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        return new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddStringArgPredicate("hasRole", "role", "Y")
                .AddConstant("a", true)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build()
        );
    }

    private static RuleCompiler<RuleTestContext> CreateCompilerWithMaxAnalysisTerms(int maxAnalysisTerms)
    {
        return new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build(),
            new CompilerOptions(MaxAnalysisTerms: maxAnalysisTerms)
        );
    }
}
