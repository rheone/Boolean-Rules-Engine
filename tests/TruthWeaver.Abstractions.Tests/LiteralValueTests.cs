namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

public sealed class LiteralValueTests
{
    [Fact]
    public void Scalar_factories_round_trip_through_their_matching_accessor()
    {
        Assert.Equal("Y", LiteralValue.OfString("Y").AsString());
        Assert.Equal(42L, LiteralValue.OfInt64(42).AsInt64());
        Assert.Equal(1.5m, LiteralValue.OfDecimal(1.5m).AsDecimal());
        Assert.True(LiteralValue.OfBoolean(true).AsBoolean());

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Assert.Equal(now, LiteralValue.OfDateTimeOffset(now).AsDateTimeOffset());

        Guid guid = Guid.NewGuid();
        Assert.Equal(guid, LiteralValue.OfGuid(guid).AsGuid());
    }

    [Fact]
    public void Reading_an_accessor_for_the_wrong_kind_throws()
    {
        LiteralValue stringValue = LiteralValue.OfString("Y");

        Assert.Throws<InvalidOperationException>(() => stringValue.AsInt64());
        Assert.Throws<InvalidOperationException>(() => stringValue.AsArray());
    }

    [Fact]
    public void String_comparison_is_case_sensitive()
    {
        LiteralValue lower = LiteralValue.OfString("y");
        LiteralValue upper = LiteralValue.OfString("Y");

        Assert.NotEqual(lower, upper);
    }

    [Fact]
    public void Array_construction_rejects_an_element_that_does_not_match_the_declared_kind()
    {
        Assert.Throws<ArgumentException>(() =>
            LiteralValue.OfArray(LiteralKind.String, [LiteralValue.OfString("a"), LiteralValue.OfInt64(1)])
        );
    }

    [Fact]
    public void Array_construction_rejects_an_array_kind_as_the_declared_element_kind()
    {
        Assert.Throws<ArgumentException>(() => LiteralValue.OfArray(LiteralKind.StringArray, []));
    }

    [Fact]
    public void Arrays_are_order_sensitive_and_report_their_elements_via_as_array()
    {
        LiteralValue left = LiteralValue.OfArray(LiteralKind.Int64, [LiteralValue.OfInt64(1), LiteralValue.OfInt64(2)]);
        LiteralValue right = LiteralValue.OfArray(LiteralKind.Int64, [LiteralValue.OfInt64(2), LiteralValue.OfInt64(1)]);

        Assert.NotEqual(left, right);
        Assert.Equal([1L, 2L], left.AsArray().Select(v => v.AsInt64()));
    }

    [Theory]
    [InlineData(LiteralKind.String, LiteralKind.StringArray)]
    [InlineData(LiteralKind.Int64, LiteralKind.Int64Array)]
    [InlineData(LiteralKind.Decimal, LiteralKind.DecimalArray)]
    [InlineData(LiteralKind.Boolean, LiteralKind.BooleanArray)]
    [InlineData(LiteralKind.DateTimeOffset, LiteralKind.DateTimeOffsetArray)]
    [InlineData(LiteralKind.Guid, LiteralKind.GuidArray)]
    public void ToArrayKind_and_ToElementKind_are_inverses_for_every_scalar_kind(LiteralKind scalar, LiteralKind array)
    {
        Assert.Equal(array, LiteralValue.ToArrayKind(scalar));
        Assert.Equal(scalar, LiteralValue.ToElementKind(array));
    }

    [Fact]
    public void ToArrayKind_rejects_an_array_kind_as_input()
    {
        Assert.Throws<ArgumentException>(() => LiteralValue.ToArrayKind(LiteralKind.StringArray));
    }

    [Fact]
    public void ToElementKind_rejects_a_scalar_kind_as_input()
    {
        Assert.Throws<ArgumentException>(() => LiteralValue.ToElementKind(LiteralKind.String));
    }

    [Fact]
    public void ToString_renders_a_string_literal_as_a_quoted_dsl_literal_with_escapes()
    {
        LiteralValue value = LiteralValue.OfString("a \"quoted\" \\ value");

        Assert.Equal("\"a \\\"quoted\\\" \\\\ value\"", value.ToString());
    }

    [Fact]
    public void ToString_renders_a_boolean_literal_lowercase()
    {
        Assert.Equal("true", LiteralValue.OfBoolean(true).ToString());
        Assert.Equal("false", LiteralValue.OfBoolean(false).ToString());
    }

    [Fact]
    public void ToString_renders_an_array_literal_as_a_bracketed_comma_separated_list()
    {
        LiteralValue array = LiteralValue.OfArray(LiteralKind.Int64, [LiteralValue.OfInt64(1), LiteralValue.OfInt64(2)]);

        Assert.Equal("[1, 2]", array.ToString());
    }
}
