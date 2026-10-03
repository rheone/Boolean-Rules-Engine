namespace TruthWeaver.Parsing;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Diagnostics;

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
        "EQUIVALENT",
        "IFF",
        "XNOR",
        "IMPLIES",
        "NAND",
        "NOR",
        "TRUE",
        "FALSE",
        "UNKNOWN",
        "NXOR",
        "ANY",
        "ALL",
        "NONE",
        "BETWEEN",
        "COALESCE",
        "IF",
        "ISTRUE",
        "ISFALSE",
        "ISUNKNOWN",
        "ISKNOWN",
        "EXACTLYONE",
        "ATLEAST",
        "ATMOST",
        "GREATERTHAN",
        "LESSTHAN",
        "EXACTLY",
    };

    // Infix operators that sit outside the NOT > AND > OR precedence chain: they may not be mixed with
    // each other or with AND/OR at one nesting level without parentheses (ADR-0005 decision 8).
    // COALESCE is infix only as the symbol ?? (the word is a function call), and, being associative, a chain of it
    // folds into one n-ary node instead of being rejected like the binary-only operators.
    private static readonly string[] InfixOperators = ["XOR", "EQUIVALENT", "IMPLIES", "NAND", "NOR", "COALESCE"];

    // The inspection function calls, each taking exactly one operand (checked by the compiler).
    private static readonly (string Keyword, InspectionKind Kind)[] InspectionKeywords =
    [
        ("ISTRUE", InspectionKind.IsTrue),
        ("ISFALSE", InspectionKind.IsFalse),
        ("ISUNKNOWN", InspectionKind.IsUnknown),
        ("ISKNOWN", InspectionKind.IsKnown),
    ];

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
        RuleNode root = parser.ParseExpression();
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

    /// <summary>Gets the spelling used in diagnostics for an infix operator: its symbol for COALESCE, else its name.</summary>
    private static string DisplayName(string infixOperator)
    {
        return infixOperator == "COALESCE" ? "??" : infixOperator;
    }

    /// <summary>
    /// Maps a symbolic operator to the named operator it aliases, so the rest of the parser only ever
    /// reasons about named operators and notation can never change the resulting tree.
    /// </summary>
    private static string? SymbolAlias(string symbol)
    {
        return symbol switch
        {
            "&&" or "∧" => "AND",
            "||" or "∨" => "OR",
            "!" or "¬" => "NOT",
            "⊕" => "XOR",
            "→" => "IMPLIES",
            "↔" => "EQUIVALENT",
            "↑" => "NAND",
            "↓" => "NOR",
            "??" => "COALESCE",
            _ => null,
        };
    }

    /// <summary>
    /// Maps a word alias to the canonical operator it stands for: <c>IFF</c> and the legacy <c>XNOR</c> both mean
    /// <c>EQUIVALENT</c> (ADR-0005 decision 5), so persisted rules written with <c>XNOR</c> keep compiling.
    /// </summary>
    private static string? WordAlias(string word)
    {
        return word.ToUpperInvariant() switch
        {
            "IFF" or "XNOR" => "EQUIVALENT",
            _ => null,
        };
    }

    private bool IsKeyword(string keyword)
    {
        return this.Current.Kind switch
        {
            TokenKind.Identifier => string.Equals(this.Current.Text, keyword, StringComparison.OrdinalIgnoreCase)
                || WordAlias(this.Current.Text) == keyword,
            TokenKind.Operator => SymbolAlias(this.Current.Text) == keyword,
            _ => false,
        };
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

    /// <summary>
    /// Parses one complete expression: an <c>OR</c>-level expression, optionally the condition of the ternary
    /// <c>condition ? whenTrue : whenFalse</c>. The ternary has the lowest precedence and, like every infix operator
    /// outside <c>NOT</c> &gt; <c>AND</c> &gt; <c>OR</c>, may not be mixed with another at one level without parentheses
    /// (ADR-0005 decision 8): its condition and both branches must each be a single operand or a parenthesized group.
    /// Every place that accepts a full expression (the root, parentheses, call arguments) goes through here.
    /// </summary>
    private RuleNode ParseExpression()
    {
        int start = this.position;
        (RuleNode condition, string? bareInfix) = this.ParseOrExpression();
        return this.Current.Kind == TokenKind.Question ? this.ParseTernaryTail(condition, bareInfix, start) : condition;
    }

    /// <summary>
    /// Parses <c>? whenTrue : whenFalse</c> after an already parsed condition. A condition or branch that is a bare
    /// <c>AND</c>/<c>OR</c> chain or infix expression, or that is itself an unparenthesized ternary, is reported as
    /// ambiguous at its own span; a nested ternary is still parsed so later problems surface in the same pass.
    /// </summary>
    private RuleNode ParseTernaryTail(RuleNode condition, string? conditionBare, int conditionStart, bool checkCondition = true)
    {
        if (checkCondition)
        {
            this.ReportBareTernaryPart(condition, conditionBare, conditionStart, "condition");
        }

        this.position++; // The '?'.
        int trueStart = this.position;
        (RuleNode whenTrue, string? trueBare) = this.ParseOrExpression();
        this.ReportBareTernaryPart(whenTrue, trueBare, trueStart, "true branch");
        if (this.Current.Kind == TokenKind.Question)
        {
            whenTrue = this.ParseNestedTernary(whenTrue, trueStart);
        }

        this.Expect(TokenKind.Colon, "':'");
        int falseStart = this.position;
        (RuleNode whenFalse, string? falseBare) = this.ParseOrExpression();
        this.ReportBareTernaryPart(whenFalse, falseBare, falseStart, "false branch");
        if (this.Current.Kind == TokenKind.Question)
        {
            whenFalse = this.ParseNestedTernary(whenFalse, falseStart);
        }

        return new IfNode([condition, whenTrue, whenFalse], SpanCovering(condition.Span.Start, whenFalse.Span.End));
    }

    /// <summary>Reports an unparenthesized ternary nested in a branch (<c>a ? b : c ? d : e</c>) and parses it for recovery.</summary>
    private RuleNode ParseNestedTernary(RuleNode nestedCondition, int nestedStart)
    {
        string message =
            "Mixing ?: with another ?: at the same level requires explicit parentheses. "
            + "Add parentheses around the nested conditional to say how the branches group.";
        this.ReportAmbiguousMixing(this.Current.Span, message);

        // The nested condition was already checked as the enclosing branch.
        return this.ParseTernaryTail(nestedCondition, null, nestedStart, checkCondition: false);
    }

    /// <summary>
    /// Reports a ternary operand that sits at the same level as the ternary without parentheses when it is an infix
    /// expression or a bare <c>AND</c>/<c>OR</c> chain. <paramref name="startToken"/> is the index of the operand's first
    /// token, used to tell <c>a AND b</c> from <c>(a AND b)</c> (the node does not retain its parentheses).
    /// </summary>
    private void ReportBareTernaryPart(RuleNode part, string? bareInfix, int startToken, string role)
    {
        string? bare = bareInfix is not null
            ? DisplayName(bareInfix)
            : part switch
            {
                AndNode when !this.IsWrappedInParentheses(startToken) => "AND",
                OrNode when !this.IsWrappedInParentheses(startToken) => "OR",
                _ => null,
            };
        if (bare is not null)
        {
            string message =
                $"Mixing ?: with {bare} at the same level requires explicit parentheses. "
                + $"Add parentheses around the {bare} expression used as the {role} to say which operator applies first.";
            this.ReportAmbiguousMixing(part.Span, message);
        }
    }

    /// <summary>
    /// Whether the tokens from <paramref name="startToken"/> up to the cursor are one parenthesized group: they open with
    /// '(' whose matching ')' is the last token consumed (so <c>(a) AND (b)</c> is not wrapped).
    /// </summary>
    private bool IsWrappedInParentheses(int startToken)
    {
        if (this.tokens[startToken].Kind != TokenKind.LParen)
        {
            return false;
        }

        int depth = 0;
        for (int i = startToken; i < this.position; i++)
        {
            depth += this.tokens[i].Kind switch
            {
                TokenKind.LParen => 1,
                TokenKind.RParen => -1,
                _ => 0,
            };
            if (depth == 0)
            {
                return i == this.position - 1;
            }
        }

        return false;
    }

    private (RuleNode Node, string? BareInfix) ParseOrExpression()
    {
        return this.ParseAndOrChain("OR", this.ParseAndExpression, (operands, span) => new OrNode(operands, span));
    }

    private (RuleNode Node, string? BareInfix) ParseAndExpression()
    {
        return this.ParseAndOrChain("AND", this.ParseInfixChain, (operands, span) => new AndNode(operands, span));
    }

    /// <summary>
    /// Parses one <c>AND</c> or <c>OR</c> level. Any operand that is a bare (unparenthesized) infix
    /// expression such as <c>a XOR b</c> is ambiguous next to <c>AND</c>/<c>OR</c> (ADR-0005 decision 8), so
    /// it is reported at its own span. A level with a single operand is passed through so an enclosing
    /// level can still see that the expression is a bare infix one.
    /// </summary>
    private (RuleNode Node, string? BareInfix) ParseAndOrChain(
        string keyword,
        Func<(RuleNode Node, string? BareInfix)> parseOperand,
        Func<List<RuleNode>, SourceSpan, RuleNode> construct
    )
    {
        (RuleNode node, string? bareInfix) = parseOperand();
        List<RuleNode> operands = [node];
        List<(RuleNode Operand, string Operator)> bareOperands = [];
        if (bareInfix is not null)
        {
            bareOperands.Add((node, bareInfix));
        }

        while (this.TryConsumeKeyword(keyword))
        {
            (RuleNode next, string? nextBare) = parseOperand();
            operands.Add(next);
            if (nextBare is not null)
            {
                bareOperands.Add((next, nextBare));
            }
        }

        if (operands.Count == 1)
        {
            return (operands[0], bareInfix);
        }

        foreach ((RuleNode operand, string infixOperator) in bareOperands)
        {
            string shown = DisplayName(infixOperator);
            string message =
                $"Mixing {shown} with AND/OR at the same level requires explicit parentheses. "
                + $"Add parentheses around the {shown} expression to say which operator applies first.";
            this.ReportAmbiguousMixing(operand.Span, message);
        }

        return (construct(operands, SpanCovering(node.Span.Start, operands[^1].Span.End)), null);
    }

    /// <summary>
    /// Parses a chain of one infix operator other than <c>AND</c>/<c>OR</c> (<c>a XOR b</c>). Operands
    /// are <c>NOT</c>-level expressions. A second, different infix operator in the same chain is ambiguous
    /// and is reported at that operator's own token (ADR-0005 decision 8).
    /// </summary>
    /// <returns>The chain's node and, when it is a real chain, the operator that built it.</returns>
    private (RuleNode Node, string? BareInfix) ParseInfixChain()
    {
        RuleNode node = this.ParseNotExpression();
        if (this.CurrentInfixOperator() is not { } chainOperator)
        {
            return (node, null);
        }

        List<RuleNode> operands = [node];
        int start = node.Span.Start;
        while (this.CurrentInfixOperator() is { } current)
        {
            if (current != chainOperator)
            {
                string message =
                    $"Mixing {DisplayName(chainOperator)} with {DisplayName(current)} at the same level requires explicit parentheses. "
                    + "Add parentheses around the operands that should be grouped first.";
                this.ReportAmbiguousMixing(this.Current.Span, message);
            }

            this.position++;
            operands.Add(this.ParseNotExpression());
        }

        SourceSpan span = SpanCovering(start, operands[^1].Span.End);
        RuleNode result = chainOperator switch
        {
            "XOR" => new XorNode(operands, span),
            "EQUIVALENT" => new EquivalentNode(operands, span),
            "IMPLIES" => new ImpliesNode(operands, span),
            "NAND" => new NandNode(operands, span),
            "NOR" => new NorNode(operands, span),
            "COALESCE" => new CoalesceNode(operands, span),
            _ => throw new InvalidOperationException($"Unhandled infix operator '{chainOperator}'."),
        };
        return (result, chainOperator);
    }

    /// <summary>Gets the canonical name of the infix operator (other than AND/OR) at the cursor, or <see langword="null"/>.</summary>
    private string? CurrentInfixOperator()
    {
        // The word COALESCE is a function call, so only its ?? symbol token counts as an infix operator.
        return InfixOperators.FirstOrDefault(op =>
            this.IsKeyword(op) && (op != "COALESCE" || this.Current.Kind == TokenKind.Operator)
        );
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

    private ConstantNode ConsumeConstant(TruthValue value)
    {
        SourceSpan span = this.Current.Span;
        this.position++;
        return new ConstantNode(value, span);
    }

    private RuleNode ParsePrimary()
    {
        if (this.Current.Kind == TokenKind.LParen)
        {
            this.position++;
            RuleNode inner = this.ParseExpression();
            this.Expect(TokenKind.RParen, "')'");
            return inner;
        }

        // Keyword matching is case-insensitive (IsKeyword), so True/TRUE/true all yield the same node.
        if (this.IsKeyword("TRUE"))
        {
            return this.ConsumeConstant(TruthValue.True);
        }

        if (this.IsKeyword("FALSE"))
        {
            return this.ConsumeConstant(TruthValue.False);
        }

        if (this.IsKeyword("UNKNOWN"))
        {
            return this.ConsumeConstant(TruthValue.Unknown);
        }

        if (this.IsKeyword("EXACTLYONE"))
        {
            return this.ParseExactlyOne();
        }

        if (this.IsKeyword("NXOR"))
        {
            return this.ParseOperandCall((operands, span) => new NxorNode(operands, span));
        }

        if (this.IsKeyword("ANY"))
        {
            return this.ParseOperandCall((operands, span) => new AnyNode(operands, span));
        }

        if (this.IsKeyword("ALL"))
        {
            return this.ParseOperandCall((operands, span) => new AllNode(operands, span));
        }

        if (this.IsKeyword("NONE"))
        {
            return this.ParseOperandCall((operands, span) => new NoneNode(operands, span));
        }

        if (this.Current.Kind == TokenKind.Identifier && this.IsKeyword("COALESCE"))
        {
            return this.ParseOperandCall((operands, span) => new CoalesceNode(operands, span));
        }

        foreach ((string keyword, InspectionKind kind) in InspectionKeywords)
        {
            if (this.IsKeyword(keyword))
            {
                return this.ParseOperandCall((operands, span) => new InspectionNode(kind, operands, span));
            }
        }

        if (this.IsKeyword("IF"))
        {
            return this.ParseOperandCall((operands, span) => new IfNode(operands, span));
        }

        if (this.IsKeyword("BETWEEN"))
        {
            return this.ParseBetween();
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

    /// <summary>
    /// Parses a keyword followed by a parenthesized, comma-separated operand list (<c>NXOR</c>, <c>ANY</c>, <c>ALL</c>,
    /// <c>NONE</c>) and wraps it with <paramref name="create"/>. The operand count is checked later by the compiler.
    /// </summary>
    private RuleNode ParseOperandCall(Func<IReadOnlyList<RuleNode>, SourceSpan, RuleNode> create)
    {
        int start = this.Current.Span.Start;
        this.position++;
        List<RuleNode> operands = this.ParseParenthesizedOperandList();
        return create(operands, SpanCovering(start, this.tokens[this.position - 1].Span.End));
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
            operands.Add(this.ParseExpression());
        }

        int end = this.Current.Span.End;
        this.Expect(TokenKind.RParen, "')'");
        return new ThresholdNode(comparison, k, operands, SpanCovering(start, end));
    }

    /// <summary>
    /// Parses <c>BETWEEN(min, max, operand, ...)</c>: two integer bounds followed by the operands. The bounds' range and
    /// the operand count are validated by the compiler, like the threshold family's <c>k</c>.
    /// </summary>
    private RuleNode ParseBetween()
    {
        int start = this.Current.Span.Start;
        this.position++;
        this.Expect(TokenKind.LParen, "'('");
        int min = this.ParseIntegerBound("minimum");
        this.Expect(TokenKind.Comma, "','");
        int max = this.ParseIntegerBound("maximum");

        List<RuleNode> operands = [];
        while (this.Current.Kind == TokenKind.Comma)
        {
            this.position++;
            operands.Add(this.ParseExpression());
        }

        int end = this.Current.Span.End;
        this.Expect(TokenKind.RParen, "')'");
        return new BetweenNode(min, max, operands, SpanCovering(start, end));
    }

    /// <summary>
    /// Reads one integer literal for a BETWEEN bound. A missing or non-integer (for example <c>1.5</c>) bound is a
    /// syntax error; a non-integer number token is still consumed so parsing can continue.
    /// </summary>
    private int ParseIntegerBound(string which)
    {
        if (
            this.Current.Kind == TokenKind.NumberLiteral
            && int.TryParse(this.Current.Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value)
        )
        {
            this.position++;
            return value;
        }

        this.diagnostics.Add(
            Diagnostic.Error(
                DiagnosticCodes.SyntaxError,
                $"Expected an integer {which} as BETWEEN's {(which == "minimum" ? "first" : "second")} argument.",
                this.Current.Span
            )
        );
        if (this.Current.Kind == TokenKind.NumberLiteral)
        {
            this.position++;
        }

        return 0;
    }

    private List<RuleNode> ParseParenthesizedOperandList()
    {
        this.Expect(TokenKind.LParen, "'('");
        List<RuleNode> operands = [];
        if (this.Current.Kind != TokenKind.RParen)
        {
            operands.Add(this.ParseExpression());
            while (this.Current.Kind == TokenKind.Comma)
            {
                this.position++;
                operands.Add(this.ParseExpression());
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

    private void ReportAmbiguousMixing(SourceSpan span, string message)
    {
        this.diagnostics.Add(Diagnostic.Error(DiagnosticCodes.AmbiguousOperatorMixing, message, span));
    }
}
