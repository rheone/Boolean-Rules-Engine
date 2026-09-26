namespace BooleanRulesEngine.Tests;

using BooleanRulesEngine.Compilation;
using BooleanRulesEngine.Diagnostics;
using BooleanRulesEngine.Evaluation;
using BooleanRulesEngine.Registry;
using BooleanRulesEngine.Tests.TestSupport;
using BooleanRulesEngine.Yaml;

/// <summary>Ticket 08: YAML tree parse/print.</summary>
public sealed class YamlTreeTests
{
    private const string WorkedExampleYaml = """
        op: and
        operands:
          - predicate: hasRole
            args:
              role: "Y"
          - op: or
            operands:
              - predicate: hasTraining
                args:
                  training: "Q"
              - predicate: hasTraining
                args:
                  training: "Z"
              - op: xor
                operands:
                  - predicate: isManager
                  - predicate: isDepartmentHead
        """;

    private const string WorkedExampleDsl =
        "hasRole(role: \"Y\") AND (hasTraining(training: \"Q\") OR hasTraining(training: \"Z\") OR (isManager XOR isDepartmentHead))";

    [Fact]
    public void Yaml_worked_example_parses_structurally_equal_to_dsl_and_json_forms()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> fromYaml = compiler.CompileYaml(WorkedExampleYaml).CompiledRule!;
        CompiledRule<RuleTestContext> fromDsl = compiler.Compile(WorkedExampleDsl).CompiledRule!;

        Assert.Equal(fromDsl.CanonicalText, fromYaml.CanonicalText);
    }

    [Fact]
    public void Compiled_rule_prints_to_yaml_and_reparses_to_a_structurally_equal_tree()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> original = compiler.Compile(WorkedExampleDsl).CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<RuleTestContext> reparsed = compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    [Fact]
    public void A_string_argument_that_reads_like_a_boolean_stays_a_string_when_quoted()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddStringArgPredicate("hasCode", "code", "true").Build()
        );

        CompilationResult<RuleTestContext> result = compiler.CompileYaml(
            """
            predicate: hasCode
            args:
              code: "true"
            """
        );

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("not: [valid, yaml: [")]
    [InlineData("op: bogus\noperands: []")]
    public void Malformed_yaml_produces_a_diagnostic_not_an_exception(string yaml)
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.CompileYaml(yaml);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        return new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddStringArgPredicate("hasRole", "role", "Y")
                .AddStringArgPredicate("hasTraining", "training", "Q")
                .AddConstant("isManager", true)
                .AddConstant("isDepartmentHead", true)
                .Build()
        );
    }
}
