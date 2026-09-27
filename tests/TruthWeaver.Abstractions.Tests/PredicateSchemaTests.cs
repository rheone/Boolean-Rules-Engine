namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

public sealed class PredicateSchemaTests
{
    [Fact]
    public void NoArguments_creates_a_schema_with_an_empty_argument_list()
    {
        PredicateSchema schema = PredicateSchema.NoArguments("isManager", "Is Manager", "Is the current user a manager?");

        Assert.Equal("isManager", schema.Name);
        Assert.Equal("Is Manager", schema.Label);
        Assert.Equal("Is the current user a manager?", schema.Description);
        Assert.Empty(schema.Arguments);
    }

    [Fact]
    public void An_argument_schema_defaults_to_required_with_no_default_value()
    {
        PredicateArgumentSchema argument = new("role", "The role code to check for.", LiteralKind.String);

        Assert.True(argument.Required);
        Assert.Null(argument.Default);
    }

    [Fact]
    public void An_optional_argument_schema_carries_its_default_value()
    {
        PredicateArgumentSchema argument = new(
            "count",
            "How many are required.",
            LiteralKind.Int64,
            Required: false,
            Default: LiteralValue.OfInt64(1)
        );

        Assert.False(argument.Required);
        Assert.Equal(LiteralValue.OfInt64(1), argument.Default);
    }
}
