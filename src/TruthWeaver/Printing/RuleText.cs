namespace TruthWeaver.Printing;

using System.Text;
using TruthWeaver.Parsing;

/// <summary>
/// Text-level tools for DSL rule text that work on the text as written, without compiling it, so they also tidy rules
/// that do not compile yet and need no predicate registry.
/// </summary>
public static class RuleText
{
    // The prefix and infix operator words. A '(' right after one of these starts a group, so it keeps a space before it
    // ("a AND (b)"); a '(' right after any other word starts a call or a term's arguments, which hug the name ("ANY(a)").
    private static readonly HashSet<string> OperatorWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "AND",
        "OR",
        "NOT",
        "XOR",
        "EQUIVALENT",
        "IFF",
        "XNOR",
        "IMPLIES",
        "NAND",
        "NOR",
    };

    /// <summary>
    /// Normalises the whitespace of DSL rule text: every run of whitespace (spaces, tabs, newlines) between tokens
    /// becomes one space, the ends are trimmed, each operator has one space on each side (the ternary's <c>?</c> and
    /// <c>:</c> included), a comma or an argument's colon is followed by one space and not preceded by one, and nothing
    /// pads the inside of a delimiter pair. Prefix negation (<c>!</c>, <c>¬</c>) hugs its operand and a call or term
    /// argument list hugs its name (<c>ANY(a, b)</c>).
    /// </summary>
    /// <remarks>
    /// Only whitespace changes: the tokens, their order, letter case, operator spelling (<c>AND</c> vs <c>&amp;&amp;</c>)
    /// and delimiter choice (<c>()</c> vs <c>[]</c> vs <c>{}</c>) are exactly as written, and the contents of string literals
    /// are copied untouched, so the result compiles to a tree equal to the input's. The result is a pure function of the
    /// token sequence, so any spacing of the same text gives the same output, and normalising twice changes nothing.
    /// Characters the DSL does not recognise are kept in place (as their own whitespace-separated pieces) instead of being
    /// dropped, so no input is lost; compiling the result reports them as it would the original. To also rewrite the
    /// delimiters or operators, compile the rule and print it with <c>CompiledRule.CanonicalText</c> or
    /// <c>CompiledRule.PrintRuleText</c>.
    /// </remarks>
    /// <param name="ruleText">The rule text. Need not compile.</param>
    /// <returns>The text with normalised whitespace; empty if <paramref name="ruleText"/> is empty or only whitespace.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="ruleText"/> is <see langword="null"/>.</exception>
    public static string NormalizeWhitespace(string ruleText)
    {
        ArgumentNullException.ThrowIfNull(ruleText);

        List<Piece> pieces = Split(ruleText);
        StringBuilder builder = new(ruleText.Length);

        // One counter per open group: how many '?' still wait for their ':' at that level. It tells a ternary's colon
        // (spaced like an operator) from an argument's colon ("name: value"), which look the same to the lexer.
        List<int> pendingQuestions = [0];
        Piece? previous = null;
        foreach (Piece piece in pieces)
        {
            bool isArgumentColon = false;
            switch (piece.Kind)
            {
                case TokenKind.LParen or TokenKind.LBracket or TokenKind.LBrace:
                    pendingQuestions.Add(0);
                    break;
                case TokenKind.RParen or TokenKind.RBracket or TokenKind.RBrace:
                    if (pendingQuestions.Count > 1)
                    {
                        pendingQuestions.RemoveAt(pendingQuestions.Count - 1);
                    }

                    break;
                case TokenKind.Question:
                    pendingQuestions[^1]++;
                    break;
                case TokenKind.Colon when pendingQuestions[^1] > 0:
                    pendingQuestions[^1]--;
                    break;
                case TokenKind.Colon:
                    isArgumentColon = true;
                    break;
            }

            if (previous is { } before && NeedsSpace(before, piece, isArgumentColon))
            {
                builder.Append(' ');
            }

            builder.Append(piece.Text);
            previous = piece;
        }

        return builder.ToString();
    }

    private static bool NeedsSpace(Piece previous, Piece current, bool currentIsArgumentColon)
    {
        if (
            previous.Kind is TokenKind.LParen or TokenKind.LBracket or TokenKind.LBrace
            || current.Kind is TokenKind.RParen or TokenKind.RBracket or TokenKind.RBrace
            || current.Kind == TokenKind.Comma
            || currentIsArgumentColon
        )
        {
            return false;
        }

        if (previous.Kind == TokenKind.Operator && previous.Text is "!" or "¬")
        {
            return false;
        }

        // A call's or term's argument list hugs its name; a group after an operator word keeps its space.
        return !(
            current.Kind == TokenKind.LParen && previous.Kind == TokenKind.Identifier && !OperatorWords.Contains(previous.Text)
        );
    }

    /// <summary>
    /// Splits the text into tokens plus any characters the lexer skipped (kept as pieces with no token kind), in source
    /// order. Token text is taken from the source, not the token, because a string token holds the decoded value.
    /// </summary>
    private static List<Piece> Split(string ruleText)
    {
        IReadOnlyList<Token> tokens = new Lexer(ruleText).Tokenize();
        List<Piece> pieces = [];
        int cursor = 0;
        foreach (Token token in tokens)
        {
            AddUnrecognised(ruleText[cursor..token.Span.Start], pieces);
            if (token.Kind != TokenKind.Eof)
            {
                pieces.Add(new Piece(token.Kind, ruleText.Substring(token.Span.Start, token.Span.Length)));
            }

            cursor = token.Span.End;
        }

        return pieces;
    }

    private static void AddUnrecognised(string gap, List<Piece> pieces)
    {
        foreach (string word in gap.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            pieces.Add(new Piece(null, word));
        }
    }

    private readonly record struct Piece(TokenKind? Kind, string Text);
}
