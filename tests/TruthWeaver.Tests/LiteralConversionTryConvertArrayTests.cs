namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;

/// <summary>
/// Ticket 25: <see cref="LiteralConversion.TryConvert"/>'s array-conversion failure paths — a
/// non-array (or elements-less) raw literal presented against an array-typed <see cref="LiteralKind"/>,
/// and an array literal whose elements don't all match the expected element kind.
/// </summary>
public sealed class LiteralConversionTryConvertArrayTests
{
    private static readonly SourceSpan Span = new(0, 0);

    [Fact]
    public void TryConvert_against_array_kind_with_non_array_raw_literal_fails_without_throwing()
    {
        RawLiteral raw = RawLiteral.OfNumber("1", Span);

        bool result = LiteralConversion.TryConvert(raw, LiteralKind.Int64Array, out LiteralValue value);

        Assert.False(result);
        Assert.Equal(default, value);
    }

    [Fact]
    public void TryConvert_against_array_kind_with_null_elements_fails_without_throwing()
    {
        RawLiteral raw = new(RawLiteralForm.Array, default, default, default, Span);

        bool result = LiteralConversion.TryConvert(raw, LiteralKind.StringArray, out LiteralValue value);

        Assert.False(result);
        Assert.Equal(default, value);
    }

    [Fact]
    public void TryConvert_against_array_kind_with_a_mismatched_element_fails_and_exposes_no_partial_value()
    {
        RawLiteral raw = RawLiteral.OfArray([RawLiteral.OfNumber("1", Span), RawLiteral.OfString("not-a-number", Span)], Span);

        bool result = LiteralConversion.TryConvert(raw, LiteralKind.Int64Array, out LiteralValue value);

        Assert.False(result);
        Assert.Equal(default, value);
    }
}
