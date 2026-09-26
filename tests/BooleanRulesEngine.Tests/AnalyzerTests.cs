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
    public void Analysis_covers_every_operator_not_just_and_or_not(string variant)
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        string rule = variant switch
        {
            "XOR" => "a XOR NOT a",
            "ExactlyOne" => "ExactlyOne(a, NOT a)",
            _ => "AtLeast(1, a, NOT a)",
        };

        CompilationResult<RuleTestContext> result = compiler.Compile(rule);

        Assert.True(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.StructuralTautology);
    }

    [Fact]
    public void Diagnostics_are_warning_or_info_never_error()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.Compile("hasRole(role: \"Y\") AND NOT hasRole(role: \"Y\")");

        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
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
}
