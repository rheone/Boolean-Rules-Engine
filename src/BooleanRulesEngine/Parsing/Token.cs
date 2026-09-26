namespace BooleanRulesEngine.Parsing;

using BooleanRulesEngine.Diagnostics;

/// <summary>The kind of a lexical token in the DSL.</summary>
internal enum TokenKind
{
    /// <summary>A bare word: a keyword (AND/OR/NOT/XOR/true/false/ExactlyOne/AtLeast) or a predicate name.</summary>
    Identifier,

    /// <summary>A double-quoted string literal.</summary>
    StringLiteral,

    /// <summary>A numeric literal (integral or decimal, sign optional).</summary>
    NumberLiteral,

    /// <summary>'('.</summary>
    LParen,

    /// <summary>')'.</summary>
    RParen,

    /// <summary>'['.</summary>
    LBracket,

    /// <summary>']'.</summary>
    RBracket,

    /// <summary>','.</summary>
    Comma,

    /// <summary>':'.</summary>
    Colon,

    /// <summary>End of input.</summary>
    Eof,
}

/// <summary>One lexical token, with its source span and (for literals) decoded text.</summary>
/// <param name="Kind">The token's kind.</param>
/// <param name="Text">The token's raw or decoded text (identifier name, or a literal's value text).</param>
/// <param name="Span">The token's location in the source text.</param>
internal readonly record struct Token(TokenKind Kind, string Text, SourceSpan Span);
