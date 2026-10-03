namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Structured diagnostics for malformed DSL (ADR-0005 decision 11, k3-conformance 28): every diagnostic carries its
/// stable code and location, the expected-versus-found pair where it applies, and a "did you mean" suggestion where one
/// can be computed deterministically.
/// </summary>
public sealed class StructuredDiagnosticsTests
{
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .AddConstant("isManager", true)
            .AddConstant("ab1", true)
            .AddConstant("ab2", true)
            .AddStringArgPredicate("hasRole", "role", "Y")
            .Build()
    );

    /// <summary>An unknown predicate name close to a registered one is answered with that registered name.</summary>
    [Fact]
    public void Compile_UnknownPredicateNearARegisteredName_SuggestsTheRegisteredName_Test()
    {
        Diagnostic diagnostic = Single("isManger");

        Assert.Equal(DiagnosticCodes.UnknownPredicate, diagnostic.Code);
        Assert.Equal(new DiagnosticSuggestion(DiagnosticSuggestionKind.Replacement, "isManager"), diagnostic.Suggestion);
    }

    /// <summary>An unknown name that is a typo of an operator keyword is answered with the keyword.</summary>
    [Fact]
    public void Compile_UnknownPredicateNearAnOperator_SuggestsTheOperator_Test()
    {
        Diagnostic diagnostic = Single("ANDD");

        Assert.Equal(new DiagnosticSuggestion(DiagnosticSuggestionKind.Replacement, "AND"), diagnostic.Suggestion);
    }

    /// <summary>A name nowhere near any known name gets no suggestion rather than a wild guess.</summary>
    [Fact]
    public void Compile_UnknownPredicateFarFromEveryKnownName_HasNoSuggestion_Test()
    {
        Diagnostic diagnostic = Single("zzzzzzzzzz");

        Assert.Null(diagnostic.Suggestion);
    }

    /// <summary>Equally close candidates resolve to the ordinally first, so the suggestion never depends on registration order.</summary>
    [Fact]
    public void Compile_UnknownPredicateEquallyCloseToTwoNames_SuggestsTheOrdinallyFirst_Test()
    {
        Diagnostic diagnostic = Single("ab3");

        Assert.Equal("ab1", diagnostic.Suggestion?.Text);
    }

    /// <summary>A misspelled word operator between operands is answered with the operator, and says what was expected.</summary>
    [Fact]
    public void Compile_MisspelledInfixOperator_SuggestsTheOperatorAndReportsExpectedAndFound_Test()
    {
        Diagnostic diagnostic = Single("a ANDD b");

        Assert.Equal(DiagnosticCodes.SyntaxError, diagnostic.Code);
        Assert.Equal(new SourceSpan(2, 4), diagnostic.Span);
        Assert.Equal("an operator or the end of the rule", diagnostic.Expected);
        Assert.Equal("'ANDD'", diagnostic.Found);
        Assert.Equal(new DiagnosticSuggestion(DiagnosticSuggestionKind.Replacement, "AND"), diagnostic.Suggestion);
    }

    /// <summary>A legacy alias misspelling resolves to the alias itself when that is the nearest word.</summary>
    [Fact]
    public void Compile_MisspelledAlias_SuggestsTheAlias_Test()
    {
        Diagnostic diagnostic = Single("a XNORR b");

        Assert.Equal("XNOR", diagnostic.Suggestion?.Text);
    }

    /// <summary>Two swapped letters count as one edit, so a transposed operator still resolves to the operator.</summary>
    [Fact]
    public void Compile_TransposedOperatorLetters_SuggestsTheOperator_Test()
    {
        Diagnostic diagnostic = Single("a XRO b");

        Assert.Equal("XOR", diagnostic.Suggestion?.Text);
    }

    /// <summary>A lone <c>&amp;</c> is answered with the doubled symbolic form.</summary>
    [Fact]
    public void Compile_LoneAmpersand_SuggestsTheDoubledSymbol_Test()
    {
        Diagnostic diagnostic = Compiler.Compile("a & b").Diagnostics[0];

        Assert.Equal("'&'", diagnostic.Found);
        Assert.Equal("&&", diagnostic.Suggestion?.Text);
    }

    /// <summary>The no-mixing rule gives its parentheses advice as a structured hint that shows the grouped text.</summary>
    [Fact]
    public void Compile_BareInfixOperandNextToAnd_HintsAtTheParenthesizedText_Test()
    {
        Diagnostic diagnostic = Single("a XOR b AND c");

        Assert.Equal(DiagnosticCodes.AmbiguousOperatorMixing, diagnostic.Code);
        Assert.Equal(DiagnosticSuggestionKind.Hint, diagnostic.Suggestion?.Kind);
        Assert.Contains("(a XOR b)", diagnostic.Suggestion?.Text);
    }

    /// <summary>Mixing two different infix operators also carries a parentheses hint.</summary>
    [Fact]
    public void Compile_TwoDifferentInfixOperators_HintsAtParentheses_Test()
    {
        Diagnostic diagnostic = Single("a XOR b XNOR c");

        Assert.Equal(DiagnosticSuggestionKind.Hint, diagnostic.Suggestion?.Kind);
        Assert.Contains("parentheses", diagnostic.Suggestion?.Text);
    }

    /// <summary>A three-operand XOR reports its arity and points at NXOR and ExactlyOne.</summary>
    [Fact]
    public void Compile_XorWithThreeOperands_ReportsArityAndHintsAtNxor_Test()
    {
        Diagnostic diagnostic = Single("a XOR b XOR c");

        Assert.Equal(DiagnosticCodes.InfixArityViolation, diagnostic.Code);
        Assert.Equal("2 operands", diagnostic.Expected);
        Assert.Equal("3 operands", diagnostic.Found);
        Assert.Equal(DiagnosticSuggestionKind.Hint, diagnostic.Suggestion?.Kind);
        Assert.Contains("NXOR", diagnostic.Suggestion?.Text);
    }

    /// <summary>A chained NAND reports its arity with a nesting hint instead of an NXOR hint.</summary>
    [Fact]
    public void Compile_NandChain_ReportsArityAndHintsAtParentheses_Test()
    {
        Diagnostic diagnostic = Single("a NAND b NAND c");

        Assert.Equal("2 operands", diagnostic.Expected);
        Assert.Equal("3 operands", diagnostic.Found);
        Assert.DoesNotContain("NXOR", diagnostic.Suggestion?.Text);
        Assert.Contains("parentheses", diagnostic.Suggestion?.Text);
    }

    /// <summary>A call with too many operands reports the expected and found operand counts.</summary>
    [Fact]
    public void Compile_InspectionWithTwoOperands_ReportsOperandCounts_Test()
    {
        Diagnostic diagnostic = Single("IsTrue(a, b)");

        Assert.Equal(DiagnosticCodes.MalformedTree, diagnostic.Code);
        Assert.Equal("1 operand", diagnostic.Expected);
        Assert.Equal("2 operands", diagnostic.Found);
    }

    /// <summary>A variadic operator with too few operands reports the minimum it needs.</summary>
    [Fact]
    public void Compile_AnyWithOneOperand_ReportsTheMinimumOperandCount_Test()
    {
        Diagnostic diagnostic = Single("ANY(a)");

        Assert.Equal("at least 2 operands", diagnostic.Expected);
        Assert.Equal("1 operand", diagnostic.Found);
    }

    /// <summary>A missing operand after an operator reports what a term would have been and that the rule ended.</summary>
    [Fact]
    public void Compile_MissingOperandAtEndOfRule_ReportsExpectedAndFound_Test()
    {
        Diagnostic diagnostic = Compiler.Compile("a AND").Diagnostics[0];

        Assert.Equal(DiagnosticCodes.SyntaxError, diagnostic.Code);
        Assert.Equal("a term, constant or '('", diagnostic.Expected);
        Assert.Equal("end of rule", diagnostic.Found);
    }

    /// <summary>A mismatched closing delimiter says which closer was expected and which was found.</summary>
    [Fact]
    public void Compile_MismatchedClosingDelimiter_ReportsExpectedAndFoundCloser_Test()
    {
        Diagnostic diagnostic = Compiler.Compile("(a AND b]").Diagnostics[0];

        Assert.Equal(DiagnosticCodes.SyntaxError, diagnostic.Code);
        Assert.Equal("')'", diagnostic.Expected);
        Assert.Equal("']'", diagnostic.Found);
        Assert.Equal(new SourceSpan(8, 1), diagnostic.Span);
    }

    /// <summary>An unclosed delimiter is reported at its opener, expecting the closer and finding the end of the rule.</summary>
    [Fact]
    public void Compile_UnclosedDelimiter_ReportsAtTheOpener_Test()
    {
        Diagnostic diagnostic = Compiler.Compile("(a AND b").Diagnostics[0];

        Assert.Equal(new SourceSpan(0, 1), diagnostic.Span);
        Assert.Equal("')'", diagnostic.Expected);
        Assert.Equal("end of rule", diagnostic.Found);
    }

    /// <summary>A closer with no opener reports that no group is open.</summary>
    [Fact]
    public void Compile_UnmatchedClosingDelimiter_ReportsFoundCloser_Test()
    {
        Diagnostic diagnostic = Compiler.Compile("a)").Diagnostics[0];

        Assert.Equal("')'", diagnostic.Found);
        Assert.Equal("the end of the rule", diagnostic.Expected);
    }

    /// <summary>An unterminated string literal says a closing quote was expected before the end of the rule.</summary>
    [Fact]
    public void Compile_UnterminatedString_ReportsExpectedClosingQuote_Test()
    {
        Diagnostic diagnostic = Compiler.Compile("hasRole(role: \"x").Diagnostics[0];

        Assert.Equal(DiagnosticCodes.SyntaxError, diagnostic.Code);
        Assert.Equal("a closing '\"'", diagnostic.Expected);
        Assert.Equal("end of rule", diagnostic.Found);
    }

    /// <summary>A bad escape sequence names the supported escapes as what was expected.</summary>
    [Fact]
    public void Compile_InvalidEscapeSequence_ReportsSupportedEscapes_Test()
    {
        Diagnostic diagnostic = Compiler.Compile("hasRole(role: \"x\\q\")").Diagnostics[0];

        Assert.Equal(DiagnosticCodes.InvalidEscapeSequence, diagnostic.Code);
        Assert.Equal("one of \\\", \\\\, \\n, \\t", diagnostic.Expected);
        Assert.Equal("\\q", diagnostic.Found);
    }

    /// <summary>A non-numeric threshold reports that an integer was expected.</summary>
    [Fact]
    public void Compile_NonIntegerThreshold_ReportsExpectedIntegerAndFoundToken_Test()
    {
        Diagnostic diagnostic = Compiler.Compile("AtLeast(x, a, b)").Diagnostics[0];

        Assert.Equal("an integer", diagnostic.Expected);
        Assert.Equal("'x'", diagnostic.Found);
    }

    /// <summary>A missing argument value reports that a literal was expected.</summary>
    [Fact]
    public void Compile_MissingArgumentLiteral_ReportsExpectedLiteral_Test()
    {
        Diagnostic diagnostic = Compiler.Compile("hasRole(role: )").Diagnostics[0];

        Assert.Equal("a literal value", diagnostic.Expected);
        Assert.Equal("')'", diagnostic.Found);
    }

    /// <summary>A nested Collapse explains the boundary rule and hints at moving it outside.</summary>
    [Fact]
    public void Compile_NestedCollapse_ReportsExpectedAndFoundAndHintsAtMovingIt_Test()
    {
        Diagnostic diagnostic = Single("a AND Collapse(b, UnknownAsFalse)");

        Assert.Equal(DiagnosticCodes.NestedCollapse, diagnostic.Code);
        Assert.Equal("Collapse as the outermost expression", diagnostic.Expected);
        Assert.Equal("Collapse nested inside another expression", diagnostic.Found);
        Assert.Equal(DiagnosticSuggestionKind.Hint, diagnostic.Suggestion?.Kind);
    }

    /// <summary>Tokens after a complete expression are reported with the expectation of an operator or the end.</summary>
    [Fact]
    public void Compile_TrailingTokens_ReportsExpectedAndFound_Test()
    {
        Diagnostic diagnostic = Compiler.Compile("a b").Diagnostics[0];

        Assert.Equal(DiagnosticCodes.SyntaxError, diagnostic.Code);
        Assert.Equal("an operator or the end of the rule", diagnostic.Expected);
        Assert.Equal("'b'", diagnostic.Found);
        Assert.Null(diagnostic.Suggestion);
    }

    /// <summary>A bad Collapse policy lists the valid policies and suggests the nearest one.</summary>
    [Fact]
    public void Compile_MisspelledCollapsePolicy_SuggestsTheNearestPolicy_Test()
    {
        Diagnostic diagnostic = Compiler.Compile("Collapse(a, UnknownAsFals)").Diagnostics[0];

        Assert.Equal("UnknownAsFalse", diagnostic.Suggestion?.Text);
        Assert.Equal("'UnknownAsFals'", diagnostic.Found);
    }

    /// <summary>A missing predicate argument names the argument that was expected.</summary>
    [Fact]
    public void Compile_MissingRequiredArgument_ReportsTheArgumentExpected_Test()
    {
        Diagnostic diagnostic = Single("hasRole");

        Assert.Equal(DiagnosticCodes.MissingArgument, diagnostic.Code);
        Assert.Equal("argument 'role'", diagnostic.Expected);
        Assert.Equal("no arguments", diagnostic.Found);
    }

    /// <summary>An unknown argument name is answered with the nearest declared argument.</summary>
    [Fact]
    public void Compile_UnknownArgumentName_SuggestsTheDeclaredArgument_Test()
    {
        Diagnostic diagnostic = Compiler.Compile("hasRole(rol: \"x\")").Diagnostics[0];

        Assert.Equal(DiagnosticCodes.UnknownArgument, diagnostic.Code);
        Assert.Equal("role", diagnostic.Suggestion?.Text);
    }

    /// <summary>Compiles <paramref name="text"/> and returns its only error diagnostic.</summary>
    private static Diagnostic Single(string text)
    {
        return Assert.Single(Compiler.Compile(text).Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }
}
