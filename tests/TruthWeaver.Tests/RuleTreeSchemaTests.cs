namespace TruthWeaver.Tests;

using System.Text.Json;
using global::Json.Schema;

/// <summary>
/// Validates the published ADR-0003 rule-tree JSON Schema (<c>rule-tree.schema.json</c>) against the
/// compiler's own JSON fixtures: every structurally valid tree shape should validate, and every
/// structurally malformed one (the same fixtures <see cref="JsonTreeTests"/> exercises against
/// <c>JsonTreeParser</c>) should fail validation.
/// </summary>
public sealed class RuleTreeSchemaTests
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

    private static readonly JsonSchema Schema = JsonSchema.FromFile(SchemaFilePath());

    public static TheoryData<string> ValidFixtures()
    {
        return new()
        {
            WorkedExampleJson,
            """{"const": true}""",
            """{"const": false}""",
            """{"const": "unknown"}""",
            """{"const": "Unknown"}""",
            """{"const": "TRUE"}""",
            """{"predicate": "isManager"}""",
            """{"predicate": "hasRole", "args": {"role": "Y"}}""",
            """{"predicate": "hasAge", "args": {"age": 42}}""",
            """{"predicate": "hasBalance", "args": {"balance": 1.5}}""",
            """{"predicate": "isActive", "args": {"active": true}}""",
            """{"predicate": "hasId", "args": {"id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"}}""",
            """{"predicate": "hasExpiry", "args": {"expiry": "2025-01-01T00:00:00.0000000+00:00"}}""",
            """{"predicate": "hasAnyRole", "args": {"roles": ["Y", "Z"]}}""",
            """{"op": "not", "operands": [{"const": true}]}""",
            """{"op": "isTrue", "operands": [{"const": "unknown"}]}""",
            """{"op": "isFalse", "operands": [{"const": true}]}""",
            """{"op": "isUnknown", "operands": [{"const": false}]}""",
            """{"op": "isKnown", "operands": [{"op": "not", "operands": [{"const": true}]}]}""",
            """{"op": "xnor", "operands": [{"predicate": "isManager"}, {"predicate": "isDepartmentHead"}]}""",
            """{"op": "equivalent", "operands": [{"predicate": "isManager"}, {"predicate": "isDepartmentHead"}]}""",
            """{"op": "iff", "operands": [{"predicate": "isManager"}, {"predicate": "isDepartmentHead"}]}""",
            """{"op": "nand", "operands": [{"predicate": "isManager"}, {"predicate": "isDepartmentHead"}]}""",
            """{"op": "nor", "operands": [{"predicate": "isManager"}, {"predicate": "isDepartmentHead"}]}""",
            """{"op": "implies", "operands": [{"predicate": "isManager"}, {"predicate": "isDepartmentHead"}]}""",
            """{"op": "nxor", "operands": [{"const": true}, {"const": false}, {"const": true}]}""",
            """{"op": "any", "operands": [{"const": true}, {"const": false}]}""",
            """{"op": "all", "operands": [{"const": true}, {"const": false}]}""",
            """{"op": "none", "operands": [{"const": true}, {"const": false}]}""",
            """{"op": "coalesce", "operands": [{"const": "unknown"}, {"const": false}]}""",
            """{"op": "if", "operands": [{"const": "unknown"}, {"const": true}, {"const": false}]}""",
            """{"op": "between", "min": 1, "max": 2, "operands": [{"const": true}, {"const": false}, {"const": true}]}""",
            """{"op": "exactlyOne", "operands": [{"const": true}, {"const": false}, {"const": true}]}""",
            """{"op": "atLeast", "k": 2, "operands": [{"const": true}, {"const": true}, {"const": false}]}""",
            """{"op": "atMost", "k": 1, "operands": [{"const": true}, {"const": false}]}""",
            """{"op": "greaterThan", "k": 1, "operands": [{"const": true}, {"const": true}]}""",
            """{"op": "lessThan", "k": 2, "operands": [{"const": true}, {"const": false}]}""",
            """{"op": "exactly", "k": 1, "operands": [{"const": true}, {"const": false}]}""",
        };
    }

    public static TheoryData<string> InvalidFixtures()
    {
        return new()
        {
            """{"op": "bogus", "operands": []}""",
            """{"nothingRecognized": true}""",
            """{"predicate": "isManager", "args": {"x": {"weird": 1}}}""",
            """{"const": "notabool"}""",
            """{"const": "maybe"}""",
            """{"const": 1}""",
            """{"predicate": 123}""",
            """{"predicate": "isManager", "args": [1, 2]}""",
            """{"op": "and"}""",
            """{"op": "not", "operands": [{"const": true}, {"const": false}]}""",
            """{"op": "isUnknown", "operands": [{"const": true}, {"const": false}]}""",
            """{"op": "isKnown", "operands": []}""",
            """{"op": "atLeast", "operands": [{"const": true}]}""",
            """{"op": "between", "min": 1, "operands": [{"const": true}, {"const": false}]}""",
            """{"op": "between", "min": "1", "max": 2, "operands": [{"const": true}, {"const": false}]}""",
            """{"op": "between", "k": 1, "operands": [{"const": true}, {"const": false}]}""",
            """{"op": "and", "min": 1, "max": 2, "operands": [{"const": true}, {"const": false}]}""",
            """{"op": "and", "operands": [1, 2]}""",
            """{"op": "and", "operands": [], "extra": "nope"}""",
        };
    }

    [Theory]
    [MemberData(nameof(ValidFixtures))]
    public void Valid_fixture_validates_against_the_schema(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);

        EvaluationResults results = Schema.Evaluate(
            document.RootElement,
            new EvaluationOptions { OutputFormat = OutputFormat.List }
        );

        Assert.True(
            results.IsValid,
            string.Join(Environment.NewLine, (results.Details ?? []).Where(d => !d.IsValid).Select(d => d.ToString()))
        );
    }

    [Theory]
    [MemberData(nameof(InvalidFixtures))]
    public void Invalid_fixture_fails_schema_validation(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);

        EvaluationResults results = Schema.Evaluate(document.RootElement);

        Assert.False(results.IsValid);
    }

    private static string SchemaFilePath()
    {
        return Path.Combine(AppContext.BaseDirectory, "TestSupport", "rule-tree.schema.json");
    }
}
