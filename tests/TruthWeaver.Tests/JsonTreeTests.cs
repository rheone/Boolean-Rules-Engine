namespace TruthWeaver.Tests;

using System.Text.Json;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

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

    [Theory]
    [InlineData("not even json", "Malformed JSON:")]
    [InlineData("""{"nothingRecognized": true}""", "must have a 'const', 'predicate', or 'op' key")]
    [InlineData("""{"const": "notabool"}""", "'const' must be a JSON boolean.")]
    [InlineData("""{"predicate": 123}""", "'predicate' must be a JSON string.")]
    [InlineData("""{"predicate": "isManager", "args": [1, 2]}""", "'args' must be a JSON object.")]
    [InlineData("""{"predicate": "isManager", "args": {"x": {"weird": 1}}}""", "Unsupported literal JSON value kind")]
    [InlineData("""{"op": "and"}""", "requires an 'operands' array")]
    [InlineData("""{"op": "not", "operands": [{"const": true}, {"const": false}]}""", "'not' requires exactly one operand.")]
    [InlineData("""{"op": "bogus", "operands": []}""", "Unknown operator 'bogus'.")]
    [InlineData("""{"op": "atLeast", "operands": [{"const": true}]}""", "requires a numeric 'k'")]
    [InlineData("""{"op": "and", "operands": [1, 2]}""", "Expected a JSON object node but found")]
    public void Every_distinct_malformed_tree_branch_raises_its_specific_message(string json, string expectedMessageSubstring)
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        CompilationResult<RuleTestContext> result = compiler.CompileJson(json);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Diagnostics,
            d =>
                d.Code == DiagnosticCodes.MalformedTree
                && d.Message.Contains(expectedMessageSubstring, StringComparison.Ordinal)
        );
    }

    [Fact]
    public void A_string_argument_containing_a_quote_round_trips_through_json()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddStringArgPredicate("hasRole", "role", "V\"IP").Build()
        );
        CompiledRule<RuleTestContext> original = compiler.Compile("hasRole(role: \"V\\\"IP\")").CompiledRule!;

        string json = original.PrintJson();
        CompiledRule<RuleTestContext> reparsed = compiler.CompileJson(json).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
        using JsonDocument document = JsonDocument.Parse(json);
        Assert.Equal("V\"IP", document.RootElement.GetProperty("args").GetProperty("role").GetString());
    }

    [Fact]
    public void A_string_argument_containing_a_backslash_round_trips_through_json()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddStringArgPredicate("hasRole", "role", "C:\\Temp").Build()
        );
        CompiledRule<RuleTestContext> original = compiler.Compile("hasRole(role: \"C:\\\\Temp\")").CompiledRule!;

        string json = original.PrintJson();
        CompiledRule<RuleTestContext> reparsed = compiler.CompileJson(json).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
        using JsonDocument document = JsonDocument.Parse(json);
        Assert.Equal("C:\\Temp", document.RootElement.GetProperty("args").GetProperty("role").GetString());
    }

    [Fact]
    public void A_string_argument_containing_both_a_quote_and_a_backslash_round_trips_through_json()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddStringArgPredicate("hasRole", "role", "V\"\\IP").Build()
        );
        CompiledRule<RuleTestContext> original = compiler.Compile("hasRole(role: \"V\\\"\\\\IP\")").CompiledRule!;

        string json = original.PrintJson();
        CompiledRule<RuleTestContext> reparsed = compiler.CompileJson(json).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
        using JsonDocument document = JsonDocument.Parse(json);
        Assert.Equal("V\"\\IP", document.RootElement.GetProperty("args").GetProperty("role").GetString());
    }

    [Fact]
    public void Compiling_a_json_element_subtree_is_structurally_equal_to_compiling_the_same_tree_as_standalone_text()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        using JsonDocument document = JsonDocument.Parse(
            $$"""
            {
              "metadata": { "name": "example", "version": 1 },
              "rule": {{WorkedExampleJson}}
            }
            """
        );

        CompiledRule<RuleTestContext> fromElement = compiler
            .CompileJson(document.RootElement.GetProperty("rule"))
            .CompiledRule!;
        CompiledRule<RuleTestContext> fromText = compiler.CompileJson(WorkedExampleJson).CompiledRule!;

        Assert.Equal(fromText.CanonicalText, fromElement.CanonicalText);
    }

    [Fact]
    public void Two_sibling_rule_expressions_in_one_document_compile_independently_with_no_cross_talk()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        using JsonDocument document = JsonDocument.Parse(
            """
            {
              "first": { "predicate": "isManager" },
              "second": { "op": "bogus", "operands": [] }
            }
            """
        );

        CompilationResult<RuleTestContext> firstResult = compiler.CompileJson(document.RootElement.GetProperty("first"));
        CompilationResult<RuleTestContext> secondResult = compiler.CompileJson(document.RootElement.GetProperty("second"));

        Assert.True(firstResult.Succeeded);
        Assert.Empty(firstResult.Diagnostics);
        Assert.False(secondResult.Succeeded);
        Assert.Contains(
            secondResult.Diagnostics,
            d =>
                d.Code == DiagnosticCodes.MalformedTree
                && d.Message.Contains("Unknown operator 'bogus'.", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void A_malformed_json_element_produces_the_same_diagnostic_as_the_equivalent_standalone_json_text()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();
        const string malformedJson = """{"nothingRecognized": true}""";
        using JsonDocument document = JsonDocument.Parse(malformedJson);

        CompilationResult<RuleTestContext> fromElement = compiler.CompileJson(document.RootElement);
        CompilationResult<RuleTestContext> fromText = compiler.CompileJson(malformedJson);

        Assert.False(fromElement.Succeeded);
        Assert.Equal(
            fromText.Diagnostics.Select(d => (d.Code, d.Message)),
            fromElement.Diagnostics.Select(d => (d.Code, d.Message))
        );
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
