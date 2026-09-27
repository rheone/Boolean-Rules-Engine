namespace TruthWeaver.Parsing;

using TruthWeaver.Diagnostics;

/// <summary>
/// The shape a literal took in the original source, before the compiler resolves it against a
/// predicate's declared <c>PredicateArgumentSchema</c>. This indirection is what lets a single
/// validation routine serve the DSL, JSON, and YAML parsers alike (tickets 04/07/08): each surface
/// produces the same <see cref="RawLiteral"/> shape from its own native syntax, and only one place
/// (<c>RuleNodeCompiler</c>) knows how to reconcile a raw literal against an expected
/// <c>LiteralKind</c> — including the DSL's only way to write a <c>DateTimeOffset</c>, a quoted
/// string the schema says to parse as a date rather than keep as text.
/// </summary>
internal enum RawLiteralForm
{
    /// <summary>A quoted string (DSL) or JSON/YAML string scalar.</summary>
    QuotedString,

    /// <summary>An unquoted number (DSL) or JSON/YAML numeric scalar.</summary>
    Number,

    /// <summary>The bare keyword <c>true</c>/<c>false</c> (DSL) or a JSON/YAML boolean scalar.</summary>
    Boolean,

    /// <summary>A bracketed/sequence list of raw literals, all of the same element form.</summary>
    Array,
}

/// <summary>A single unresolved literal value, in whichever surface form it was written.</summary>
/// <param name="Form">Which native form this literal took.</param>
/// <param name="Text">The literal's text (for <see cref="RawLiteralForm.QuotedString"/> or <see cref="RawLiteralForm.Number"/>).</param>
/// <param name="BooleanValue">The literal's value (for <see cref="RawLiteralForm.Boolean"/>).</param>
/// <param name="Elements">The literal's elements (for <see cref="RawLiteralForm.Array"/>).</param>
/// <param name="Span">The literal's location in source text.</param>
internal sealed record RawLiteral(
    RawLiteralForm Form,
    string? Text,
    bool BooleanValue,
    IReadOnlyList<RawLiteral>? Elements,
    SourceSpan Span
)
{
    public static RawLiteral OfString(string text, SourceSpan span)
    {
        return new(RawLiteralForm.QuotedString, text, default, default, span);
    }

    public static RawLiteral OfNumber(string text, SourceSpan span)
    {
        return new(RawLiteralForm.Number, text, default, default, span);
    }

    public static RawLiteral OfBoolean(bool value, SourceSpan span)
    {
        return new(RawLiteralForm.Boolean, default, value, default, span);
    }

    public static RawLiteral OfArray(IReadOnlyList<RawLiteral> elements, SourceSpan span)
    {
        return new(RawLiteralForm.Array, default, default, elements, span);
    }
}
