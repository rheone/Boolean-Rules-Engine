namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Ticket 22: rule text is printed with every run of whitespace collapsed to one space, trimmed, with a space around
/// each operator, for any input spacing. <see cref="RuleText.NormalizeWhitespace"/> does this to text as written (keeping
/// the author's operators, case and delimiters), and the canonical printer already produces the same tidy spacing.
/// </summary>
public sealed class WhitespaceNormalisationTests
{
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .AddConstant("d", true)
            .Add(
                new PredicateSchema(
                    "named",
                    "Named",
                    "Has a name.",
                    [new PredicateArgumentSchema("value", "The name.", LiteralKind.String)]
                ),
                (_, _, _) => ValueTask.FromResult(TruthValue.True)
            )
            .Add(
                new PredicateSchema(
                    "ids",
                    "Ids",
                    "Has ids.",
                    [new PredicateArgumentSchema("value", "The ids.", LiteralKind.Int64Array)]
                ),
                (_, _, _) => ValueTask.FromResult(TruthValue.True)
            )
            .Build()
    );

    /// <summary>Runs of spaces, tabs and newlines collapse to one space and the ends are trimmed.</summary>
    [Theory]
    [InlineData("  a   AND\t\tb\r\n OR  c \n", "a AND b OR c")]
    [InlineData("a\nAND\r\nb", "a AND b")]
    [InlineData("   ", "")]
    [InlineData("", "")]
    public void NormalizeWhitespace_AnyWhitespaceRuns_CollapsesToSingleSpacesAndTrims_Test(string text, string expected)
    {
        Assert.Equal(expected, RuleText.NormalizeWhitespace(text));
    }

    /// <summary>Symbolic operators get a space on each side, however tightly they were written; prefix negation hugs its operand.</summary>
    [Theory]
    [InlineData("a&&b||c", "a && b || c")]
    [InlineData("a∧b∨¬c", "a ∧ b ∨ ¬c")]
    [InlineData("a⊕b→c", "a ⊕ b → c")]
    [InlineData("a  &&   !  b", "a && !b")]
    [InlineData("a??b", "a ?? b")]
    [InlineData("a?b:c", "a ? b : c")]
    [InlineData("a   ?b  :   c", "a ? b : c")]
    public void NormalizeWhitespace_SymbolicOperators_AreSurroundedBySingleSpaces_Test(string text, string expected)
    {
        Assert.Equal(expected, RuleText.NormalizeWhitespace(text));
    }

    /// <summary>Commas are followed by one space, argument colons likewise, and nothing pads the inside of a delimiter pair.</summary>
    [Theory]
    [InlineData("ANY( a ,b,  c )", "ANY(a, b, c)")]
    [InlineData("named( value :\"x\" )", "named(value: \"x\")")]
    [InlineData("ids(value:[1 ,2,  3])", "ids(value: [1, 2, 3])")]
    [InlineData("a AND ( b OR [ c ] )", "a AND (b OR [c])")]
    [InlineData("a AND{b}", "a AND {b}")]
    [InlineData("a AND(b)", "a AND (b)")]
    [InlineData("NOT   (a)", "NOT (a)")]
    [InlineData("named  (value: \"x\")", "named(value: \"x\")")]
    public void NormalizeWhitespace_PunctuationAndCalls_UsesTheCanonicalLayout_Test(string text, string expected)
    {
        Assert.Equal(expected, RuleText.NormalizeWhitespace(text));
    }

    /// <summary>A ternary's colon is spaced like an operator while an argument's colon hugs its name, even when nested together.</summary>
    [Fact]
    public void NormalizeWhitespace_TernaryContainingAnArgumentColon_SpacesEachColonByItsRole_Test()
    {
        string normalized = RuleText.NormalizeWhitespace("a?named(value:\"x\"):b");

        Assert.Equal("a ? named(value: \"x\") : b", normalized);
    }

    /// <summary>Whitespace and delimiter characters inside a string literal are the literal's value and never change.</summary>
    [Fact]
    public void NormalizeWhitespace_StringLiteral_IsKeptVerbatim_Test()
    {
        string normalized = RuleText.NormalizeWhitespace("named(value:  \"  a \\\" ,  ) \t b  \")");

        Assert.Equal("named(value: \"  a \\\" ,  ) \t b  \")", normalized);
    }

    /// <summary>Letter case, operator spelling and delimiter choice are the author's and are not rewritten.</summary>
    [Fact]
    public void NormalizeWhitespace_Content_KeepsCaseOperatorsAndDelimiters_Test()
    {
        string normalized = RuleText.NormalizeWhitespace("a  and [b  XNOR  {c}]");

        Assert.Equal("a and [b XNOR {c}]", normalized);
    }

    /// <summary>Text the lexer would reject is kept in place rather than dropped, so normalising never loses input.</summary>
    [Fact]
    public void NormalizeWhitespace_UnrecognisedCharacters_AreKeptInPlace_Test()
    {
        Assert.Equal("a # b", RuleText.NormalizeWhitespace("  a   #   b "));
    }

    /// <summary>Normalising already-normalised text changes nothing.</summary>
    [Theory]
    [InlineData("a && !b || (c ? d : a)")]
    [InlineData("ANY(a, b, [c AND d])")]
    [InlineData("named(value: \"x  y\") XOR ids(value: [1, 2])")]
    public void NormalizeWhitespace_AlreadyNormalisedText_IsUnchanged_Test(string text)
    {
        Assert.Equal(text, RuleText.NormalizeWhitespace(text));
    }

    /// <summary>The normalised text of any spacing of the same rule is identical and compiles to the same tree.</summary>
    [Theory]
    [InlineData("a AND (b OR c)", "  a\tAND\r\n(  b   OR c)  ")]
    [InlineData("ANY(a, b, c)", "ANY (a,b ,\n c)")]
    [InlineData("named(value: \"x y\") ? a : b", "named( value:\"x y\" )?a:b")]
    [InlineData("a && !b", "a&&  !b")]
    public void NormalizeWhitespace_DifferentSpacingsOfOneRule_ProduceIdenticalTextAndTrees_Test(string tidy, string messy)
    {
        string normalizedMessy = RuleText.NormalizeWhitespace(messy);

        Assert.Equal(RuleText.NormalizeWhitespace(tidy), normalizedMessy);
        Assert.Equal(Compile(messy).Root, Compile(normalizedMessy).Root);
    }

    /// <summary>The canonical text is single-spaced, trimmed and spaced around operators and after commas whatever the input spacing.</summary>
    [Theory]
    [InlineData("  a\t\tAND\r\n b  ", "a AND b")]
    [InlineData("a&&b||c", "(a AND b) OR c")]
    [InlineData("ANY(a,b ,\n c)", "ANY(a, b, c)")]
    [InlineData("a   XOR\tb", "(a XOR b)")]
    [InlineData("  named( value :\"x\" )  ", "named(value: \"x\")")]
    [InlineData("a?b:c", "If(a, b, c)")]
    public void CanonicalText_AnyInputSpacing_IsSingleSpacedAndTrimmed_Test(string text, string expected)
    {
        Assert.Equal(expected, Compile(text).CanonicalText);
    }

    private static CompiledRule<RuleTestContext> Compile(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        return result.CompiledRule!;
    }
}
