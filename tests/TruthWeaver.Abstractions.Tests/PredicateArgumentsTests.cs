namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

public sealed class PredicateArgumentsTests
{
    [Fact]
    public void Typed_getters_return_the_supplied_value()
    {
        PredicateArguments args = new(
            new Dictionary<string, LiteralValue>
            {
                ["role"] = LiteralValue.OfString("Y"),
                ["count"] = LiteralValue.OfInt64(3),
                ["ratio"] = LiteralValue.OfDecimal(0.5m),
                ["active"] = LiteralValue.OfBoolean(true),
            }
        );

        Assert.Equal("Y", args.GetString("role"));
        Assert.Equal(3L, args.GetInt64("count"));
        Assert.Equal(0.5m, args.GetDecimal("ratio"));
        Assert.True(args.GetBool("active"));
    }

    [Fact]
    public void Array_getters_return_elements_in_source_order()
    {
        PredicateArguments args = new(
            new Dictionary<string, LiteralValue>
            {
                ["roles"] = LiteralValue.OfArray(LiteralKind.String, [LiteralValue.OfString("a"), LiteralValue.OfString("b")]),
            }
        );

        Assert.Equal(["a", "b"], args.GetStringArray("roles"));
    }

    [Fact]
    public void Requesting_a_missing_argument_throws_key_not_found()
    {
        PredicateArguments args = PredicateArguments.Empty;

        Assert.Throws<KeyNotFoundException>(() => args.GetString("role"));
    }

    [Fact]
    public void Requesting_an_argument_with_a_typed_getter_that_does_not_match_its_kind_throws()
    {
        PredicateArguments args = new(new Dictionary<string, LiteralValue> { ["role"] = LiteralValue.OfString("Y") });

        Assert.Throws<InvalidOperationException>(() => args.GetInt64("role"));
    }

    [Fact]
    public void GetGuid_returns_the_supplied_value()
    {
        Guid value = Guid.NewGuid();
        PredicateArguments args = new(new Dictionary<string, LiteralValue> { ["id"] = LiteralValue.OfGuid(value) });

        Assert.Equal(value, args.GetGuid("id"));
    }

    [Fact]
    public void Requesting_a_kind_mismatched_argument_names_the_argument_and_both_kinds_in_the_message()
    {
        PredicateArguments args = new(new Dictionary<string, LiteralValue> { ["id"] = LiteralValue.OfString("not-a-guid") });

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => args.GetGuid("id"));

        Assert.Contains("'id'", exception.Message);
        Assert.Contains($"'{LiteralKind.String}'", exception.Message);
        Assert.Contains($"'{LiteralKind.Guid}'", exception.Message);
    }

    [Fact]
    public void GetRaw_returns_the_literal_regardless_of_kind()
    {
        LiteralValue value = LiteralValue.OfGuid(Guid.NewGuid());
        PredicateArguments args = new(new Dictionary<string, LiteralValue> { ["id"] = value });

        Assert.Equal(value, args.GetRaw("id"));
    }

    [Fact]
    public void Empty_has_no_arguments()
    {
        Assert.Throws<KeyNotFoundException>(() => PredicateArguments.Empty.GetRaw("anything"));
    }
}
