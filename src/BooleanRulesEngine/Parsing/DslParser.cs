namespace BooleanRulesEngine.Parsing;

using BooleanRulesEngine.Ast;
using BooleanRulesEngine.Diagnostics;

/// <summary>
/// Hand-written recursive-descent parser for the canonical word-operator DSL (ADR-0003): word
/// operators only, matched case-insensitively; precedence <c>NOT</c> &gt; <c>AND</c> &gt; <c>OR</c>;
/// <c>XOR</c> mixed with <c>AND</c>/<c>OR</c> at the same syntactic level without parentheses is
/// rejected rather than resolved by a precedence guess. Never throws for a syntax error — it reports
/// a <see cref="DiagnosticCodes.SyntaxError"/> diagnostic and recovers with an <see cref="ErrorNode"/>
/// so the rest of the source still gets parsed and can surface further diagnostics.
/// </summary>
internal sealed class DslParser
{
    private static readonly HashSet<string> ReservedWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "AND",
        "OR",
        "NOT",
        "XOR",
        "XNOR",
        "TRUE",
        "FALSE",
        "EXACTLYONE",
        "ATLEAST",
        "ATMOST",
        "GREATERTHAN",
        "LESSTHAN",
        "EXACTLY",
    };

    private readonly IReadOnlyList<Token> tokens;
    private readonly List<Diagnostic> diagnostics;
    private int position;

    private DslParser(IReadOnlyList<Token> tokens, List<Diagnostic> diagnostics)
    {
        this.tokens = tokens;
        this.diagnostics = diagnostics;
    }

    private Token Current => this.tokens[this.position];

    /// <summary>Determines whether a bare identifier is a reserved DSL keyword and therefore cannot be a predicate name.</summary>
    /// <param name="identifier">The identifier text.</param>
    /// <returns><see langword="true"/> if the identifier is reserved.</returns>
    public static bool IsReservedWord(string identifier)
    {
        return ReservedWords.Contains(identifier);
    }

    /// <summary>Parses DSL rule text into a raw <see cref="RuleNode"/> tree plus any diagnostics.</summary>
    /// <param name="source">The rule text.</param>
    /// <returns>The parsed root node and the diagnostics raised while parsing.</returns>
    public static (RuleNode Root, IReadOnlyList<Diagnostic> Diagnostics) Parse(string source)
    {
        Lexer lexer = new(source);
        IReadOnlyList<Token> tokens = lexer.Tokenize();
        List<Diagnostic> diagnostics = [.. lexer.Diagnostics];
        DslParser parser = new(tokens, diagnostics);
        RuleNode root = parser.ParseOrExpression().Node;
        if (parser.Current.Kind != TokenKind.Eof)
        {
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.SyntaxError,
                    $"Unexpected token '{parser.Current.Text}' after end of expression.",
                    parser.Current.Span
                )
            );
        }

        return (root, diagnostics);
    }

    private static SourceSpan SpanCovering(int start, int end)
    {
        return new(start, Math.Max(0, end - start));
    }

    private bool IsKeyword(string keyword)
    {
        return this.Current.Kind == TokenKind.Identifier
            && string.Equals(this.Current.Text, keyword, StringComparison.OrdinalIgnoreCase);
    }

    private bool TryConsumeKeyword(string keyword)
    {
        if (!this.IsKeyword(keyword))
        {
            return false;
        }

        this.position++;
        return true;
    }

    private (RuleNode Node, bool IsBareXor) ParseOrExpression()
    {
        (RuleNode node, bool isBareXor) = this.ParseAndExpression();
        List<RuleNode> operands = [node];
        bool anyBare = isBareXor;
        int start = node.Span.Start;
        while (this.TryConsumeKeyword("OR"))
        {
            (RuleNode next, bool nextBare) = this.ParseAndExpression();
            operands.Add(next);
            anyBare |= nextBare;
        }

        if (operands.Count == 1)
        {
            return (operands[0], anyBare);
        }

        SourceSpan span = SpanCovering(start, operands[^1].Span.End);
        if (anyBare)
        {
            this.ReportAmbiguousMixing(span);
        }

        return (new OrNode(operands, span), anyBare);
    }

    private (RuleNode Node, bool IsBareXor) ParseAndExpression()
    {
        (RuleNode node, bool isBareXor) = this.ParseXorChain();
        List<RuleNode> operands = [node];
        bool anyBare = isBareXor;
        int start = node.Span.Start;
        while (this.TryConsumeKeyword("AND"))
        {
            (RuleNode next, bool nextBare) = this.ParseXorChain();
            operands.Add(next);
            anyBare |= nextBare;
        }

        if (operands.Count == 1)
        {
            return (operands[0], anyBare);
        }

        SourceSpan span = SpanCovering(start, operands[^1].Span.End);
        if (anyBare)
        {
            this.ReportAmbiguousMixing(span);
        }

        return (new AndNode(operands, span), anyBare);
    }

    private (RuleNode Node, bool IsBareXor) ParseXorChain()
    {
        RuleNode node = this.ParseNotExpression();
        List<RuleNode> operands = [node];
        int start = node.Span.Start;
        bool? isXnor = null;
        while (this.IsKeyword("XOR") || this.IsKeyword("XNOR"))
        {
            bool currentIsXnor = this.IsKeyword("XNOR");
            if (isXnor is bool previous && previous != currentIsXnor)
            {
                this.ReportAmbiguousMixing(
                    SpanCovering(start, this.Current.Span.End),
                    "Mixing XOR with XNOR at the same level requires explicit parentheses."
                );
            }

            isXnor = currentIsXnor;
            this.position++;
            operands.Add(this.ParseNotExpression());
        }

        if (operands.Count == 1)
        {
            return (operands[0], false);
        }

        SourceSpan span = SpanCovering(start, operands[^1].Span.End);
        RuleNode result = isXnor == true ? new XnorNode(operands, span) : new XorNode(operands, span);
        return (result, true);
    }

    private RuleNode ParseNotExpression()
    {
        if (this.TryConsumeKeyword("NOT"))
        {
            int start = this.tokens[this.position - 1].Span.Start;
            RuleNode operand = this.ParseNotExpression();
            return new NotNode(operand, SpanCovering(start, operand.Span.End));
        }

        return this.ParsePrimary();
    }

    private RuleNode ParsePrimary()
    {
        if (this.Current.Kind == TokenKind.LParen)
        {
            this.position++;
            (RuleNode inner, _) = this.ParseOrExpression();
            this.Expect(TokenKind.RParen, "')'");
            return inner;
        }

        if (this.IsKeyword("TRUE"))
        {
            SourceSpan span = this.Current.Span;
            this.position++;
            return new ConstantNode(true, span);
        }

        if (this.IsKeyword("FALSE"))
        {
            SourceSpan span = this.Current.Span;
            this.position++;
            return new ConstantNode(false, span);
        }

        if (this.IsKeyword("EXACTLYONE"))
        {
            return this.ParseExactlyOne();
        }

        if (this.IsKeyword("ATLEAST"))
        {
            return this.ParseThreshold(ThresholdComparison.AtLeast);
        }

        if (this.IsKeyword("ATMOST"))
        {
            return this.ParseThreshold(ThresholdComparison.AtMost);
        }

        if (this.IsKeyword("GREATERTHAN"))
        {
            return this.ParseThreshold(ThresholdComparison.GreaterThan);
        }

        if (this.IsKeyword("LESSTHAN"))
        {
            return this.ParseThreshold(ThresholdComparison.LessThan);
        }

        if (this.IsKeyword("EXACTLY"))
        {
            return this.ParseThreshold(ThresholdComparison.Exactly);
        }

        if (this.Current.Kind == TokenKind.Identifier)
        {
            return this.ParseTerm();
        }

        this.diagnostics.Add(
            Diagnostic.Error(
                DiagnosticCodes.SyntaxError,
                $"Expected a term, constant, or '(' but found '{this.Current.Text}'.",
                this.Current.Span
            )
        );
        SourceSpan errorSpan = this.Current.Span;
        if (this.Current.Kind != TokenKind.Eof)
        {
            this.position++;
        }

        return new ErrorNode(errorSpan);
    }

    private RuleNode ParseTerm()
    {
        Token nameToken = this.Current;
        this.position++;
        List<ArgumentNode> arguments = [];
        int end = nameToken.Span.End;
        if (this.Current.Kind == TokenKind.LParen)
        {
            this.position++;
            if (this.Current.Kind != TokenKind.RParen)
            {
                arguments.Add(this.ParseArgument());
                while (this.Current.Kind == TokenKind.Comma)
                {
                    this.position++;
                    arguments.Add(this.ParseArgument());
                }
            }

            end = this.Current.Span.End;
            this.Expect(TokenKind.RParen, "')'");
        }

        return new TermNode(nameToken.Text, arguments, SpanCovering(nameToken.Span.Start, end));
    }

    private ArgumentNode ParseArgument()
    {
        Token nameToken = this.Current;
        this.Expect(TokenKind.Identifier, "an argument name");
        this.Expect(TokenKind.Colon, "':'");
        RawLiteral value = this.ParseLiteral();
        return new ArgumentNode(nameToken.Text, value, SpanCovering(nameToken.Span.Start, value.Span.End));
    }

    private RawLiteral ParseLiteral()
    {
        Token current = this.Current;
        switch (current.Kind)
        {
            case TokenKind.StringLiteral:
                this.position++;
                return RawLiteral.OfString(current.Text, current.Span);
            case TokenKind.NumberLiteral:
                this.position++;
                return RawLiteral.OfNumber(current.Text, current.Span);
            case TokenKind.Identifier when string.Equals(current.Text, "true", StringComparison.OrdinalIgnoreCase):
                this.position++;
                return RawLiteral.OfBoolean(true, current.Span);
            case TokenKind.Identifier when string.Equals(current.Text, "false", StringComparison.OrdinalIgnoreCase):
                this.position++;
                return RawLiteral.OfBoolean(false, current.Span);
            case TokenKind.LBracket:
                return this.ParseArrayLiteral();
            default:
                this.diagnostics.Add(
                    Diagnostic.Error(
                        DiagnosticCodes.SyntaxError,
                        $"Expected a literal value but found '{current.Text}'.",
                        current.Span
                    )
                );
                if (current.Kind != TokenKind.Eof)
                {
                    this.position++;
                }

                return RawLiteral.OfBoolean(false, current.Span);
        }
    }

    private RawLiteral ParseArrayLiteral()
    {
        int start = this.Current.Span.Start;
        this.position++;
        List<RawLiteral> elements = [];
        if (this.Current.Kind != TokenKind.RBracket)
        {
            elements.Add(this.ParseLiteral());
            while (this.Current.Kind == TokenKind.Comma)
            {
                this.position++;
                elements.Add(this.ParseLiteral());
            }
        }

        int end = this.Current.Span.End;
        this.Expect(TokenKind.RBracket, "']'");
        return RawLiteral.OfArray(elements, SpanCovering(start, end));
    }

    private RuleNode ParseExactlyOne()
    {
        int start = this.Current.Span.Start;
        this.position++;
        List<RuleNode> operands = this.ParseParenthesizedOperandList();
        return new ExactlyOneNode(operands, SpanCovering(start, this.tokens[this.position - 1].Span.End));
    }

    private RuleNode ParseThreshold(ThresholdComparison comparison)
    {
        int start = this.Current.Span.Start;
        this.position++;
        this.Expect(TokenKind.LParen, "'('");
        int k = 0;
        if (this.Current.Kind == TokenKind.NumberLiteral)
        {
            k = int.TryParse(this.Current.Text, out int parsed) ? parsed : 0;
            this.position++;
        }
        else
        {
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.SyntaxError,
                    $"Expected an integer threshold as {comparison}'s first argument.",
                    this.Current.Span
                )
            );
        }

        List<RuleNode> operands = [];
        while (this.Current.Kind == TokenKind.Comma)
        {
            this.position++;
            operands.Add(this.ParseOrExpression().Node);
        }

        int end = this.Current.Span.End;
        this.Expect(TokenKind.RParen, "')'");
        return new ThresholdNode(comparison, k, operands, SpanCovering(start, end));
    }

    private List<RuleNode> ParseParenthesizedOperandList()
    {
        this.Expect(TokenKind.LParen, "'('");
        List<RuleNode> operands = [];
        if (this.Current.Kind != TokenKind.RParen)
        {
            operands.Add(this.ParseOrExpression().Node);
            while (this.Current.Kind == TokenKind.Comma)
            {
                this.position++;
                operands.Add(this.ParseOrExpression().Node);
            }
        }

        this.Expect(TokenKind.RParen, "')'");
        return operands;
    }

    private void Expect(TokenKind kind, string description)
    {
        if (this.Current.Kind == kind)
        {
            this.position++;
            return;
        }

        this.diagnostics.Add(
            Diagnostic.Error(
                DiagnosticCodes.SyntaxError,
                $"Expected {description} but found '{this.Current.Text}'.",
                this.Current.Span
            )
        );
    }

    private void ReportAmbiguousMixing(
        SourceSpan span,
        string message = "Mixing XOR with AND/OR at the same level requires explicit parentheses."
    )
    {
        this.diagnostics.Add(Diagnostic.Error(DiagnosticCodes.AmbiguousOperatorMixing, message, span));
    }
}
