namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary><see cref="CompiledRule{TContext}.Describe"/>: every operator's and predicate's label/description, recursively.</summary>
public sealed class OperatorInfoTests
{
    [Theory]
    [InlineData("true", "True")]
    [InlineData("false", "False")]
    [InlineData("a AND b", "AND")]
    [InlineData("a OR b", "OR")]
    [InlineData("NOT a", "NOT")]
    [InlineData("(a XOR b)", "XOR")]
    [InlineData("(a XNOR b)", "XNOR")]
    [InlineData("ExactlyOne(a, b)", "ExactlyOne")]
    [InlineData("AtLeast(1, a, b)", "AtLeast(1)")]
    [InlineData("AtMost(1, a, b)", "AtMost(1)")]
    [InlineData("GreaterThan(0, a, b)", "GreaterThan(0)")]
    [InlineData("LessThan(2, a, b)", "LessThan(2)")]
    [InlineData("Exactly(1, a, b)", "Exactly(1)")]
    public void Every_operator_node_has_a_non_empty_label_and_description(string dsl, string expectedLabel)
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build()
        );

        RuleDescription description = Compile(compiler, dsl).Describe();

        Assert.Equal(expectedLabel, description.Label);
        Assert.False(string.IsNullOrWhiteSpace(description.Description));
    }

    [Fact]
    public void A_term_node_is_described_from_its_registered_predicate_schema()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddStringArgPredicate("hasRole", "role", "Y").Build()
        );

        RuleDescription description = Compile(compiler, "hasRole(role: \"Y\")").Describe();

        Assert.Equal("hasRole", description.Label);
        Assert.Contains("hasRole", description.Description, StringComparison.Ordinal);
        Assert.Empty(description.Operands);
    }

    [Fact]
    public void An_operator_nodes_operands_are_described_recursively()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build()
        );

        RuleDescription description = Compile(compiler, "a AND b").Describe();

        Assert.Equal("AND", description.Label);
        Assert.Equal(2, description.Operands.Count);
        Assert.Equal("a", description.Operands[0].Label);
        Assert.Equal("b", description.Operands[1].Label);
    }

    [Fact]
    public void Registry_resolves_a_predicates_label_and_description_by_name()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddStringArgPredicate("hasRole", "role", "Y")
            .Build();

        bool found = registry.TryGetSchema("hasRole", out PredicateSchema? schema);

        Assert.True(found);
        Assert.Equal("hasRole", schema!.Label);
        Assert.False(string.IsNullOrWhiteSpace(schema.Description));
    }

    [Fact]
    public void Registry_lookup_is_case_insensitive_and_reports_not_found_for_an_unregistered_name()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("isManager", true)
            .Build();

        Assert.True(registry.TryGetSchema("ISMANAGER", out PredicateSchema? found));
        Assert.NotNull(found);
        Assert.False(registry.TryGetSchema("neverRegistered", out PredicateSchema? notFound));
        Assert.Null(notFound);
    }

    private static CompiledRule<RuleTestContext> Compile(RuleCompiler<RuleTestContext> compiler, string dsl)
    {
        CompilationResult<RuleTestContext> result = compiler.Compile(dsl);
        Assert.True(result.Succeeded);
        return result.CompiledRule!;
    }
}
