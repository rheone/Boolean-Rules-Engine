namespace BooleanRulesEngine.Tests;

using BooleanRulesEngine.Compilation;
using BooleanRulesEngine.Diagnostics;
using BooleanRulesEngine.Evaluation;
using BooleanRulesEngine.Registry;
using BooleanRulesEngine.Tests.TestSupport;

/// <summary>Ticket 07: JSON tree parse/print and round-trip.</summary>
public sealed class JsonTreeTests
{
    private const string WorkedExampleJson = """
        {
          "op": "and",
          "operands": [
            { "predicate": "hasRole", "args": { "role": "Y" } },
            {
              "op": "or",
              "operands": [
                { "predicate": "hasTraining", "args": { "training": "Q" } },
                { "predicate": "hasTraining", "args": { "training": "Z" } },
                {
                  "op": "xor",
                  "operands": [
                    { "predicate": "isManager" },
                    { "predicate": "isDepartmentHead" }
                  ]
                }
              ]
            }
          ]
        }
        """;

    private const string WorkedExampleDsl =
        "hasRole(role: \"Y\") AND (hasTraining(training: \"Q\") OR hasTraining(training: \"Z\") OR (isManager XOR isDepartmentHead))";

    [Fact]
    public void Json_worked_example_parses_structurally_equal_to_the_dsl_form()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompiledRule<RuleTestContext> fromJson = compiler.CompileJson(WorkedExampleJson).CompiledRule!;
        CompiledRule<RuleTestContext> fromDsl = compiler.Compile(WorkedExampleDsl).CompiledRule!;

        Assert.Equal(fromDsl.CanonicalText, fromJson.CanonicalText);
    }

    [Theory]
    [InlineData("AtLeast", "atLeast")]
    [InlineData("AtMost", "atMost")]
    [InlineData("GreaterThan", "greaterThan")]
    [InlineData("LessThan", "lessThan")]
    [InlineData("Exactly", "exactly")]
    public void Threshold_family_round_trips_through_json(string dslKeyword, string jsonOpName)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", true)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build()
        );
        CompiledRule<RuleTestContext> original = compiler.Compile($"{dslKeyword}(2, a, b, c)").CompiledRule!;

        string json = original.PrintJson();
        CompiledRule<RuleTestContext> reparsed = compiler.CompileJson(json).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
        string compact = json.Replace(" ", string.Empty, StringComparison.Ordinal);
        Assert.Contains("\"k\":2", compact, StringComparison.Ordinal);
        Assert.Contains($"\"op\":\"{jsonOpName}\"", compact, StringComparison.Ordinal);
    }

    [Fact]
    public void Xnor_round_trips_through_json()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build()
        );
        CompiledRule<RuleTestContext> original = compiler.Compile("a XNOR b").CompiledRule!;

        string json = original.PrintJson();
        CompiledRule<RuleTestContext> reparsed = compiler.CompileJson(json).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
        Assert.Contains("\"op\":\"xnor\"", json.Replace(" ", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
    }

    [Fact]
    public void Compiled_rule_prints_to_json_and_reparses_to_a_structurally_equal_tree()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> original = compiler.Compile(WorkedExampleDsl).CompiledRule!;

        string json = original.PrintJson();
        CompiledRule<RuleTestContext> reparsed = compiler.CompileJson(json).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    [Fact]
    public void Dsl_to_ast_to_json_to_ast_is_structurally_equal_to_dsl_to_ast()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        CompiledRule<RuleTestContext> fromDsl = compiler.Compile(WorkedExampleDsl).CompiledRule!;

        CompiledRule<RuleTestContext> roundTripped = compiler.CompileJson(fromDsl.PrintJson()).CompiledRule!;

        Assert.Equal(fromDsl.CanonicalText, roundTripped.CanonicalText);
    }

    [Theory]
    [InlineData("""{"op": "bogus", "operands": []}""")]
    [InlineData("""{"nothingRecognized": true}""")]
    [InlineData("""{"predicate": "isManager", "args": {"x": {"weird": 1}}}""")]
    [InlineData("not even json")]
    public void Malformed_json_produces_a_diagnostic_not_an_exception(string json)
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.CompileJson(json);

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
