namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;

/// <summary>
/// Ticket 24: <see cref="LiteralConversion.Guess"/>'s lenient-mode fallback branches — boolean and
/// decimal-form numbers, plus <c>GuessArray</c>'s empty-array branch. These are exercised directly
/// against the raw parse-tree shape rather than through <c>RuleCompiler</c>, since the integer-form
/// number branch is already covered end-to-end via the lenient-mode compiler tests.
/// </summary>
public sealed class LiteralConversionGuessTests
{
    private static readonly SourceSpan Span = new(0, 0);

    [Fact]
    public void Guess_on_boolean_form_returns_the_corresponding_boolean_value()
    {
        RawLiteral raw = RawLiteral.OfBoolean(true, Span);

        LiteralValue value = LiteralConversion.Guess(raw);

        Assert.Equal(LiteralKind.Boolean, value.Kind);
        Assert.True(value.AsBoolean());
    }

    [Fact]
    public void Guess_on_number_form_containing_a_decimal_point_returns_a_decimal_value()
    {
        RawLiteral raw = RawLiteral.OfNumber("1.5", Span);

        LiteralValue value = LiteralConversion.Guess(raw);

        Assert.Equal(LiteralKind.Decimal, value.Kind);
        Assert.Equal(1.5m, value.AsDecimal());
    }

    [Fact]
    public void Guess_on_empty_array_form_returns_an_empty_string_array()
    {
        RawLiteral raw = RawLiteral.OfArray([], Span);

        LiteralValue value = LiteralConversion.Guess(raw);

        Assert.Equal(LiteralKind.StringArray, value.Kind);
        Assert.Empty(value.AsArray());
    }
}
