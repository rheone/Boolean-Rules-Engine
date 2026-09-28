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
    public void GetInt64Array_returns_elements_in_source_order()
    {
        PredicateArguments args = new(
            new Dictionary<string, LiteralValue>
            {
                ["counts"] = LiteralValue.OfArray(LiteralKind.Int64, [LiteralValue.OfInt64(1), LiteralValue.OfInt64(2)]),
            }
        );

        Assert.Equal([1L, 2L], args.GetInt64Array("counts"));
    }

    [Fact]
    public void GetDecimalArray_returns_elements_in_source_order()
    {
        PredicateArguments args = new(
            new Dictionary<string, LiteralValue>
            {
                ["ratios"] = LiteralValue.OfArray(
                    LiteralKind.Decimal,
                    [LiteralValue.OfDecimal(0.5m), LiteralValue.OfDecimal(1.5m)]
                ),
            }
        );

        Assert.Equal([0.5m, 1.5m], args.GetDecimalArray("ratios"));
    }

    [Fact]
    public void GetBoolArray_returns_elements_in_source_order()
    {
        PredicateArguments args = new(
            new Dictionary<string, LiteralValue>
            {
                ["flags"] = LiteralValue.OfArray(
                    LiteralKind.Boolean,
                    [LiteralValue.OfBoolean(true), LiteralValue.OfBoolean(false)]
                ),
            }
        );

        Assert.Equal([true, false], args.GetBoolArray("flags"));
    }

    [Fact]
    public void GetDateTimeOffsetArray_returns_elements_in_source_order()
    {
        DateTimeOffset first = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset second = new(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);
        PredicateArguments args = new(
            new Dictionary<string, LiteralValue>
            {
                ["dates"] = LiteralValue.OfArray(
                    LiteralKind.DateTimeOffset,
                    [LiteralValue.OfDateTimeOffset(first), LiteralValue.OfDateTimeOffset(second)]
                ),
            }
        );

        Assert.Equal([first, second], args.GetDateTimeOffsetArray("dates"));
    }

    [Fact]
    public void GetGuidArray_returns_elements_in_source_order()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        PredicateArguments args = new(
            new Dictionary<string, LiteralValue>
            {
                ["ids"] = LiteralValue.OfArray(LiteralKind.Guid, [LiteralValue.OfGuid(first), LiteralValue.OfGuid(second)]),
            }
        );

        Assert.Equal([first, second], args.GetGuidArray("ids"));
    }

    [Fact]
    public void GetStringArray_returns_an_empty_list_for_an_empty_array_argument()
    {
        PredicateArguments args = new(
            new Dictionary<string, LiteralValue> { ["roles"] = LiteralValue.OfArray(LiteralKind.String, []) }
        );

        Assert.Empty(args.GetStringArray("roles"));
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
