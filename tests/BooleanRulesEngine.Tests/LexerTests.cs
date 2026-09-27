namespace BooleanRulesEngine.Tests;

using BooleanRulesEngine.Diagnostics;
using BooleanRulesEngine.Parsing;

/// <summary>Ticket 03: direct unit tests for the DSL tokenizer.</summary>
public sealed class LexerTests
{
    [Theory]
    [InlineData("(", nameof(TokenKind.LParen))]
    [InlineData(")", nameof(TokenKind.RParen))]
    [InlineData("[", nameof(TokenKind.LBracket))]
    [InlineData("]", nameof(TokenKind.RBracket))]
    [InlineData(",", nameof(TokenKind.Comma))]
    [InlineData(":", nameof(TokenKind.Colon))]
    public void Every_punctuation_token_produces_the_correct_kind_and_a_one_character_span(
        string source,
        string expectedKindName
    )
    {
        Lexer lexer = new(source);

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Token token = tokens[0];
        Assert.Equal(expectedKindName, token.Kind.ToString());
        Assert.Equal(new SourceSpan(0, 1), token.Span);
        Assert.Equal(TokenKind.Eof, tokens[1].Kind);
    }

    [Fact]
    public void A_negative_integer_is_read_as_a_single_number_literal_token()
    {
        Lexer lexer = new("-5");

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.NumberLiteral, tokens[0].Kind);
        Assert.Equal("-5", tokens[0].Text);
        Assert.Empty(lexer.Diagnostics);
    }

    [Fact]
    public void A_bare_minus_not_followed_by_a_digit_raises_an_unexpected_character_diagnostic()
    {
        Lexer lexer = new("-");

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.Eof, tokens[0].Kind);
        Assert.Contains(lexer.Diagnostics, d => d.Message.Contains("Unexpected character"));
    }

    [Fact]
    public void A_decimal_number_is_read_as_one_number_literal_token()
    {
        Lexer lexer = new("1.5");

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.NumberLiteral, tokens[0].Kind);
        Assert.Equal("1.5", tokens[0].Text);
    }

    [Fact]
    public void A_trailing_dot_with_no_following_digit_is_not_consumed_into_the_number()
    {
        Lexer lexer = new("1.");

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.NumberLiteral, tokens[0].Kind);
        Assert.Equal("1", tokens[0].Text);
        Assert.Contains(lexer.Diagnostics, d => d.Message.Contains("Unexpected character"));
    }

    [Theory]
    [InlineData("\"a\\\"b\"", "a\"b")]
    [InlineData("\"a\\\\b\"", "a\\b")]
    [InlineData("\"a\\nb\"", "a\nb")]
    [InlineData("\"a\\tb\"", "a\tb")]
    public void Each_supported_escape_decodes_to_the_correct_character(string source, string expected)
    {
        Lexer lexer = new(source);

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.StringLiteral, tokens[0].Kind);
        Assert.Equal(expected, tokens[0].Text);
        Assert.Empty(lexer.Diagnostics);
    }

    [Fact]
    public void An_unterminated_string_raises_a_diagnostic_and_still_returns_a_string_token()
    {
        Lexer lexer = new("\"abc");

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.StringLiteral, tokens[0].Kind);
        Assert.Equal("abc", tokens[0].Text);
        Assert.Contains(lexer.Diagnostics, d => d.Message.Contains("Unterminated string literal"));
    }

    [Fact]
    public void An_unrecognized_character_raises_a_diagnostic_and_tokenization_continues_past_it()
    {
        Lexer lexer = new("a # b");

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Contains(lexer.Diagnostics, d => d.Message.Contains("Unexpected character"));
        Assert.Equal(TokenKind.Identifier, tokens[0].Kind);
        Assert.Equal("a", tokens[0].Text);
        Assert.Equal(TokenKind.Identifier, tokens[1].Kind);
        Assert.Equal("b", tokens[1].Text);
        Assert.Equal(TokenKind.Eof, tokens[^1].Kind);
    }

    [Fact]
    public void Tokenizing_empty_source_returns_only_the_eof_token()
    {
        Lexer lexer = new(string.Empty);

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Token token = Assert.Single(tokens);
        Assert.Equal(TokenKind.Eof, token.Kind);
    }

    [Fact]
    public void Tokenizing_whitespace_only_source_returns_only_the_eof_token()
    {
        Lexer lexer = new("   \t\n  ");

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Token token = Assert.Single(tokens);
        Assert.Equal(TokenKind.Eof, token.Kind);
    }

    [Fact]
    public void An_identifier_is_read_up_to_the_first_non_identifier_character()
    {
        Lexer lexer = new("abc_123(x)");

        IReadOnlyList<Token> tokens = lexer.Tokenize();

        Assert.Equal(TokenKind.Identifier, tokens[0].Kind);
        Assert.Equal("abc_123", tokens[0].Text);
        Assert.Equal(TokenKind.LParen, tokens[1].Kind);
    }
}
