namespace TruthWeaver.Ast;

using TruthWeaver.Abstractions;

/// <summary>
/// The single spelling table for K3 constants (ADR-0005 decision 11), shared by the DSL canonical printer, the
/// descriptors and the JSON/YAML printers and parsers so every layer agrees on how <c>True</c>, <c>False</c> and
/// <c>Unknown</c> are written and read.
/// </summary>
internal static class TruthValueText
{
    /// <summary>Gets the canonical (upper camel) spelling used by the DSL printer, descriptions and evaluated trees.</summary>
    /// <param name="value">The constant's value.</param>
    /// <returns><c>True</c>, <c>False</c> or <c>Unknown</c>.</returns>
    public static string Canonical(TruthValue value)
    {
        return value switch
        {
            TruthValue.True => "True",
            TruthValue.False => "False",
            TruthValue.Unknown => "Unknown",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unhandled truth value."),
        };
    }

    /// <summary>Gets the lower-case spelling written to JSON/YAML trees when a constant is not a plain boolean.</summary>
    /// <param name="value">The constant's value.</param>
    /// <returns><c>true</c>, <c>false</c> or <c>unknown</c>.</returns>
    public static string TreeFormat(TruthValue value)
    {
        return value switch
        {
            TruthValue.True => "true",
            TruthValue.False => "false",
            TruthValue.Unknown => "unknown",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unhandled truth value."),
        };
    }

    /// <summary>Attempts to read a constant's text (<c>true</c>, <c>false</c> or <c>unknown</c>) in any letter case.</summary>
    /// <param name="text">The text to read.</param>
    /// <param name="value">The constant's value when recognised.</param>
    /// <returns><see langword="true"/> if <paramref name="text"/> names a K3 constant.</returns>
    public static bool TryParse(string? text, out TruthValue value)
    {
        if (string.Equals(text, "true", StringComparison.OrdinalIgnoreCase))
        {
            value = TruthValue.True;
            return true;
        }

        if (string.Equals(text, "false", StringComparison.OrdinalIgnoreCase))
        {
            value = TruthValue.False;
            return true;
        }

        if (string.Equals(text, "unknown", StringComparison.OrdinalIgnoreCase))
        {
            value = TruthValue.Unknown;
            return true;
        }

        value = TruthValue.False;
        return false;
    }
}
