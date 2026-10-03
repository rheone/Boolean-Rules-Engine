namespace TruthWeaver.Parsing;

/// <summary>
/// The DSL's words in the spelling the documentation uses, as the candidate lists for "did you mean" suggestions. The
/// parser itself matches keywords case-insensitively; these spellings exist only so a suggestion reads the way the
/// language is documented (<c>ExactlyOne</c>, not <c>EXACTLYONE</c>).
/// </summary>
internal static class DslVocabulary
{
    /// <summary>Gets the word operators that sit between two operands, including the <c>IFF</c> and <c>XNOR</c> aliases.</summary>
    public static IReadOnlyList<string> InfixWords { get; } =
    ["AND", "OR", "XOR", "EQUIVALENT", "IFF", "XNOR", "IMPLIES", "NAND", "NOR"];

    /// <summary>Gets every keyword a term name could be a misspelling of: the infix words plus the prefix operators, calls and constants.</summary>
    public static IReadOnlyList<string> Keywords { get; } =
    [
        .. InfixWords,
        "NOT",
        "NXOR",
        "ANY",
        "ALL",
        "NONE",
        "BETWEEN",
        "COALESCE",
        "If",
        "IsTrue",
        "IsFalse",
        "IsUnknown",
        "IsKnown",
        "Project",
        "Collapse",
        "ExactlyOne",
        "AtLeast",
        "AtMost",
        "GreaterThan",
        "LessThan",
        "Exactly",
        "True",
        "False",
        "Unknown",
    ];

    /// <summary>Gets the symbolic spelling to suggest for a stray character that is half of a doubled symbol, or <see langword="null"/>.</summary>
    /// <param name="character">The unexpected character.</param>
    /// <returns><c>&amp;&amp;</c> for <c>&amp;</c>, <c>||</c> for <c>|</c>, otherwise <see langword="null"/>.</returns>
    public static string? DoubledSymbolFor(char character)
    {
        return character switch
        {
            '&' => "&&",
            '|' => "||",
            _ => null,
        };
    }
}
