namespace TruthWeaver.Compilation;

using TruthWeaver.Abstractions;
using TruthWeaver.Parsing;

/// <summary>
/// Reconciles a <see cref="RawLiteral"/> (the shape a literal took in whichever surface it was
/// parsed from) against a predicate argument's declared <see cref="LiteralKind"/>. This is the one
/// place a DSL string, a JSON scalar, and a YAML scalar all become the same <see cref="LiteralValue"/>
/// — including the DSL's only way to express a <see cref="LiteralKind.DateTimeOffset"/>: a quoted
/// string the schema says to parse as a date rather than keep as text.
/// </summary>
internal static class LiteralConversion
{
    /// <summary>Attempts to convert a raw literal to the expected kind.</summary>
    /// <param name="raw">The raw literal, as parsed.</param>
    /// <param name="expectedKind">The kind the owning predicate's schema declares.</param>
    /// <param name="value">The converted value, if conversion succeeded.</param>
    /// <returns><see langword="true"/> if <paramref name="raw"/> matches <paramref name="expectedKind"/>.</returns>
    public static bool TryConvert(RawLiteral raw, LiteralKind expectedKind, out LiteralValue value)
    {
        if (IsArrayKind(expectedKind))
        {
            return TryConvertArray(raw, expectedKind, out value);
        }

        switch (expectedKind)
        {
            case LiteralKind.String when raw.Form == RawLiteralForm.QuotedString && raw.Text is not null:
                value = LiteralValue.OfString(raw.Text);
                return true;
            case LiteralKind.Int64
                when raw.Form == RawLiteralForm.Number
                    && raw.Text?.Contains('.') == false
                    && long.TryParse(raw.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longValue):
                value = LiteralValue.OfInt64(longValue);
                return true;
            case LiteralKind.Decimal
                when raw.Form == RawLiteralForm.Number
                    && raw.Text is not null
                    && decimal.TryParse(raw.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal decimalValue):
                value = LiteralValue.OfDecimal(decimalValue);
                return true;
            case LiteralKind.Boolean when raw.Form == RawLiteralForm.Boolean:
                value = LiteralValue.OfBoolean(raw.BooleanValue);
                return true;
            case LiteralKind.DateTimeOffset
                when raw.Form == RawLiteralForm.QuotedString
                    && raw.Text is not null
                    && DateTimeOffset.TryParse(
                        raw.Text,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out DateTimeOffset dateTimeOffsetValue
                    ):
                value = LiteralValue.OfDateTimeOffset(dateTimeOffsetValue);
                return true;
            case LiteralKind.Guid
                when raw.Form == RawLiteralForm.QuotedString
                    && raw.Text is not null
                    && Guid.TryParse(raw.Text, out Guid guidValue):
                value = LiteralValue.OfGuid(guidValue);
                return true;
            default:
                value = default;
                return false;
        }
    }

    /// <summary>
    /// Best-effort infers a <see cref="LiteralValue"/> from a raw literal with no declared schema to
    /// validate against — used only for a term bound to an unregistered predicate under
    /// <c>CompilationMode.Lenient</c>, where the term will never actually be evaluated (it is
    /// permanently <c>TruthValue.Unknown</c>) but still needs a stable identity for memoization.
    /// </summary>
    /// <param name="raw">The raw literal.</param>
    /// <returns>An inferred literal value.</returns>
    public static LiteralValue Guess(RawLiteral raw)
    {
        switch (raw.Form)
        {
            case RawLiteralForm.QuotedString:
                return LiteralValue.OfString(raw.Text ?? string.Empty);
            case RawLiteralForm.Boolean:
                return LiteralValue.OfBoolean(raw.BooleanValue);
            case RawLiteralForm.Number:
                string text = raw.Text ?? "0";
                if (text.Contains('.'))
                {
                    decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal decimalValue);
                    return LiteralValue.OfDecimal(decimalValue);
                }

                long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long int64Value);
                return LiteralValue.OfInt64(int64Value);
            default:
                return GuessArray(raw.Elements ?? []);
        }
    }

    private static bool IsArrayKind(LiteralKind kind)
    {
        return kind
            is LiteralKind.StringArray
                or LiteralKind.Int64Array
                or LiteralKind.DecimalArray
                or LiteralKind.BooleanArray
                or LiteralKind.DateTimeOffsetArray
                or LiteralKind.GuidArray;
    }

    private static bool TryConvertArray(RawLiteral raw, LiteralKind expectedKind, out LiteralValue value)
    {
        if (raw.Form != RawLiteralForm.Array || raw.Elements is null)
        {
            value = default;
            return false;
        }

        LiteralKind elementKind = LiteralValue.ToElementKind(expectedKind);
        List<LiteralValue> items = new(raw.Elements.Count);
        foreach (RawLiteral element in raw.Elements)
        {
            if (!TryConvert(element, elementKind, out LiteralValue itemValue))
            {
                value = default;
                return false;
            }

            items.Add(itemValue);
        }

        value = LiteralValue.OfArray(elementKind, items);
        return true;
    }

    private static LiteralValue GuessArray(IReadOnlyList<RawLiteral> elements)
    {
        if (elements.Count == 0)
        {
            return LiteralValue.OfArray(LiteralKind.String, []);
        }

        LiteralValue first = Guess(elements[0]);
        List<LiteralValue> guessed = [first];
        for (int i = 1; i < elements.Count; i++)
        {
            LiteralValue candidate = Guess(elements[i]);
            guessed.Add(candidate.Kind == first.Kind ? candidate : LiteralValue.OfString(elements[i].Text ?? string.Empty));
        }

        try
        {
            return LiteralValue.OfArray(first.Kind, guessed);
        }
        catch (ArgumentException)
        {
            return LiteralValue.OfArray(LiteralKind.String, []);
        }
    }
}
