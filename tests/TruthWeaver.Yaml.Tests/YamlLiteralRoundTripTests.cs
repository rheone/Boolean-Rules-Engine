namespace TruthWeaver.Yaml.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Yaml;
using TruthWeaver.Yaml.Tests.TestSupport;

public sealed class YamlLiteralRoundTripTests
{
    [Fact]
    public void A_guid_argument_round_trips_through_yaml_as_a_quoted_scalar()
    {
        Guid id = Guid.NewGuid();
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>
                .CreateBuilder()
                .Add(
                    new PredicateSchema(
                        "hasId",
                        "Has Id",
                        "True iff 'id' equals the expected value.",
                        [new PredicateArgumentSchema("id", "The id to compare against.", LiteralKind.Guid)]
                    ),
                    (_, args, _) => ValueTask.FromResult(args.GetGuid("id") == id)
                )
                .Build()
        );
        CompiledRule<YamlTestContext> original = compiler.Compile($"hasId(id: \"{id}\")").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<YamlTestContext> reparsed = compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
        Assert.Contains($"\"{id}\"", yaml, StringComparison.Ordinal);
    }

    [Fact]
    public void A_datetimeoffset_argument_round_trips_through_yaml_as_a_quoted_iso8601_scalar()
    {
        DateTimeOffset cutoff = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>
                .CreateBuilder()
                .Add(
                    new PredicateSchema(
                        "isAfter",
                        "Is After",
                        "True iff 'cutoff' equals the expected value.",
                        [new PredicateArgumentSchema("cutoff", "The cutoff to compare against.", LiteralKind.DateTimeOffset)]
                    ),
                    (_, args, _) => ValueTask.FromResult(args.GetDateTimeOffset("cutoff") == cutoff)
                )
                .Build()
        );
        CompiledRule<YamlTestContext> original = compiler.Compile($"isAfter(cutoff: \"{cutoff:O}\")").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<YamlTestContext> reparsed = compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    [Fact]
    public void A_string_array_argument_round_trips_through_yaml_preserving_order()
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>
                .CreateBuilder()
                .Add(
                    new PredicateSchema(
                        "hasAnyRole",
                        "Has Any Role",
                        "True iff any of 'roles' matches.",
                        [new PredicateArgumentSchema("roles", "The role codes to check for.", LiteralKind.StringArray)]
                    ),
                    (_, args, _) => ValueTask.FromResult(args.GetStringArray("roles").Count > 0)
                )
                .Build()
        );
        CompiledRule<YamlTestContext> original = compiler.Compile("hasAnyRole(roles: [\"A\", \"B\", \"C\"])").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<YamlTestContext> reparsed = compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
        Assert.Equal("hasAnyRole(roles: [\"A\", \"B\", \"C\"])", original.CanonicalText);
    }
}
