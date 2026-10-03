namespace TruthWeaver.Tests;

using System.Text.Json;
using TruthWeaver.Abstractions;
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
        Assert.Contains(
            "\"op\":\"equivalent\"",
            json.Replace(" ", string.Empty, StringComparison.Ordinal),
            StringComparison.Ordinal
        );
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
    [InlineData("""{"const": "notabool"}""", "'const' must be a JSON boolean or one of")]
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

    [Fact]
    public void A_decimal_argument_prints_as_a_json_number()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddDecimalArgPredicate("exceedsThreshold", "threshold", 12.5m)
                .Build()
        );
        CompiledRule<RuleTestContext> original = compiler.Compile("exceedsThreshold(threshold: 12.5)").CompiledRule!;

        string json = original.PrintJson();

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement value = document.RootElement.GetProperty("args").GetProperty("threshold");
        Assert.Equal(JsonValueKind.Number, value.ValueKind);
        Assert.Equal(12.5m, value.GetDecimal());
        CompiledRule<RuleTestContext> reparsed = compiler.CompileJson(json).CompiledRule!;
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    [Fact]
    public void A_boolean_argument_prints_as_a_json_boolean()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddBooleanArgPredicate("hasFlag", "flag", true).Build()
        );
        CompiledRule<RuleTestContext> original = compiler.Compile("hasFlag(flag: true)").CompiledRule!;

        string json = original.PrintJson();

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement value = document.RootElement.GetProperty("args").GetProperty("flag");
        Assert.Equal(JsonValueKind.True, value.ValueKind);
        CompiledRule<RuleTestContext> reparsed = compiler.CompileJson(json).CompiledRule!;
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    [Fact]
    public void A_guid_argument_prints_as_a_json_string_via_its_ToString()
    {
        Guid id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddGuidArgPredicate("hasId", "id", id).Build()
        );
        CompiledRule<RuleTestContext> original = compiler.Compile($"hasId(id: \"{id}\")").CompiledRule!;

        string json = original.PrintJson();

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement value = document.RootElement.GetProperty("args").GetProperty("id");
        Assert.Equal(JsonValueKind.String, value.ValueKind);
        Assert.Equal(id.ToString(), value.GetString());
        CompiledRule<RuleTestContext> reparsed = compiler.CompileJson(json).CompiledRule!;
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    [Fact]
    public void A_datetimeoffset_argument_prints_as_a_round_trippable_o_format_json_string()
    {
        DateTimeOffset when = new(2024, 6, 1, 12, 30, 0, TimeSpan.Zero);
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddDateTimeOffsetArgPredicate("occurredAt", "when", when).Build()
        );
        CompiledRule<RuleTestContext> original = compiler.Compile($"occurredAt(when: \"{when:O}\")").CompiledRule!;

        string json = original.PrintJson();

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement value = document.RootElement.GetProperty("args").GetProperty("when");
        Assert.Equal(JsonValueKind.String, value.ValueKind);
        Assert.Equal(when, DateTimeOffset.ParseExact(value.GetString()!, "O", CultureInfo.InvariantCulture));
        CompiledRule<RuleTestContext> reparsed = compiler.CompileJson(json).CompiledRule!;
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    [Fact]
    public void An_array_kind_argument_prints_as_a_json_array_via_ArrayLiteralToNode()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .Add(
                    new PredicateSchema(
                        "hasAnyCode",
                        "hasAnyCode",
                        "True iff any of 'codes' matches.",
                        [new PredicateArgumentSchema("codes", "The codes to check for.", LiteralKind.Int64Array)]
                    ),
                    (_, args, _) =>
                        ValueTask.FromResult(args.GetInt64Array("codes").Count > 0 ? TruthValue.True : TruthValue.False)
                )
                .Build()
        );
        CompiledRule<RuleTestContext> original = compiler.Compile("hasAnyCode(codes: [1, 2, 3])").CompiledRule!;

        string json = original.PrintJson();

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement value = document.RootElement.GetProperty("args").GetProperty("codes");
        Assert.Equal(JsonValueKind.Array, value.ValueKind);
        List<long> elements = [];
#pragma warning disable IDISP004 // JsonElement's array enumerator is a disposable struct; a `foreach` loop already disposes it via its generated finally block.
        foreach (JsonElement element in value.EnumerateArray())
        {
            elements.Add(element.GetInt64());
        }
#pragma warning restore IDISP004

        Assert.Equal([1, 2, 3], elements);
        CompiledRule<RuleTestContext> reparsed = compiler.CompileJson(json).CompiledRule!;
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
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
