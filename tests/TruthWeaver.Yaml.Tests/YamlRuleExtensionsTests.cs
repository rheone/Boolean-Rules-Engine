namespace TruthWeaver.Yaml.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Yaml;
using TruthWeaver.Yaml.Tests.TestSupport;

public sealed class YamlRuleExtensionsTests
{
    [Fact]
    public void CompileYaml_compiles_a_single_term()
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>.CreateBuilder().AddConstant("isManager", true).Build()
        );

        CompilationResult<YamlTestContext> result = compiler.CompileYaml("predicate: isManager");

        Assert.True(result.Succeeded);
        Assert.Equal("isManager", result.CompiledRule!.CanonicalText);
    }

    [Fact]
    public void PrintYaml_and_CompileYaml_round_trip_a_compiled_rule()
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>
                .CreateBuilder()
                .AddConstant("isManager", true)
                .AddStringArgPredicate("hasRole", "role", "Y")
                .Build()
        );
        CompiledRule<YamlTestContext> original = compiler.Compile("isManager AND hasRole(role: \"Y\")").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<YamlTestContext> reparsed = compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    [Fact]
    public void CompileYaml_reports_a_diagnostic_rather_than_throwing_for_an_unknown_predicate()
    {
        RuleCompiler<YamlTestContext> compiler = new(PredicateRegistry<YamlTestContext>.CreateBuilder().Build());

        CompilationResult<YamlTestContext> result = compiler.CompileYaml("predicate: neverRegistered");

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Diagnostics);
    }
}
