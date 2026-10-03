namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Ticket 20: <c>()</c>, <c>[]</c> and <c>{}</c> are interchangeable grouping delimiters (ADR-0005 decision 9). The tree
/// does not retain the written delimiter, and a delimiter that is mismatched, unexpected or never closed is reported at
/// the exact token that is wrong.
/// </summary>
public sealed class GroupingDelimiterTests
{
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .AddConstant("d", true)
            .Build()
    );

    /// <summary>The same rule written with any mix of delimiters compiles to an equal tree and the same canonical text.</summary>
    [Theory]
    [InlineData("a AND (b OR c)", "a AND [b OR c]")]
    [InlineData("a AND (b OR c)", "a AND {b OR c}")]
    [InlineData("(a AND b) OR c", "[a AND b] OR c")]
    [InlineData("(a AND (b OR (c AND d)))", "(a AND [b OR {c AND d}])")]
    [InlineData("(a AND (b OR (c AND d)))", "{a AND [b OR (c AND d)]}")]
    [InlineData("NOT (a OR b)", "NOT {a OR b}")]
    [InlineData("(a XOR b) XOR c", "[a XOR b] XOR c")]
    [InlineData("ANY((a XOR b), c)", "ANY([a XOR b], {c})")]
    public void Compile_EquivalentRulesWithDifferentDelimiters_YieldEqualTreesAndCanonicalText_Test(
        string parenthesised,
        string other
    )
    {
        CompilationResult<RuleTestContext> expected = Compiler.Compile(parenthesised);
        CompilationResult<RuleTestContext> actual = Compiler.Compile(other);

        Assert.True(expected.Succeeded);
        Assert.True(actual.Succeeded, string.Join("; ", actual.Diagnostics.Select(d => d.Message)));
        Assert.Equal(expected.CompiledRule!.Root, actual.CompiledRule!.Root);
        Assert.Equal(expected.CompiledRule.CanonicalText, actual.CompiledRule.CanonicalText);
    }

    /// <summary>Square and curly groups count as parenthesised for the ternary no-mixing rule.</summary>
    [Theory]
    [InlineData("[a AND b] ? c : d")]
    [InlineData("a ? {b OR c} : [d]")]
    public void Compile_TernaryOperandInAlternateDelimiters_CountsAsParenthesised_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
    }

    /// <summary>A closer of the wrong kind is reported at that closer, naming what was expected and what was found.</summary>
    [Theory]
    [InlineData("a AND (b OR c]", 13, "Expected ')' to close '(' at offset 6 but found ']'")]
    [InlineData("a AND [b OR c)", 13, "Expected ']' to close '[' at offset 6 but found ')'")]
    [InlineData("{a AND b)", 8, "Expected '}' to close '{' at offset 0 but found ')'")]
    [InlineData("a AND (b OR [c AND d)]", 20, "Expected ']' to close '[' at offset 12 but found ')'")]
    public void Compile_MismatchedClosingDelimiter_ReportsTheClosingTokenWithExpectedAndFound_Test(
        string text,
        int offset,
        string message
    )
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        // A wrongly nested closer can also upset the enclosing group, so the first diagnostic is the one that matters.
        Diagnostic diagnostic = result.Diagnostics.First(d => d.Code == DiagnosticCodes.SyntaxError);
        Assert.Equal(new SourceSpan(offset, 1), diagnostic.Span);
        Assert.Equal(message + ".", diagnostic.Message);
    }

    /// <summary>A mismatch inside a function call's own argument list is reported the same way.</summary>
    [Fact]
    public void Compile_MismatchedClosingDelimiterInCallArguments_ReportsTheClosingToken_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile("ANY(a, b]");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.SyntaxError);
        Assert.Equal(new SourceSpan(8, 1), diagnostic.Span);
        Assert.Equal("Expected ')' to close '(' at offset 3 but found ']'.", diagnostic.Message);
    }

    /// <summary>A group that is never closed is reported at its opener, whichever delimiter opened it.</summary>
    [Theory]
    [InlineData("a AND (b OR c", 6, '(', ')')]
    [InlineData("a AND [b OR c", 6, '[', ']')]
    [InlineData("{a AND b", 0, '{', '}')]
    public void Compile_UnclosedDelimiter_ReportsTheOpenerSpan_Test(string text, int offset, char opener, char closer)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.SyntaxError);
        Assert.Equal(new SourceSpan(offset, 1), diagnostic.Span);
        Assert.Equal(
            $"Unclosed '{opener}' at offset {offset}: expected '{closer}' before the end of the rule.",
            diagnostic.Message
        );
    }

    /// <summary>A closer with no opener is reported at that closer.</summary>
    [Theory]
    [InlineData("a AND b)", 7, ')')]
    [InlineData("a AND b]", 7, ']')]
    [InlineData("a OR b}", 6, '}')]
    [InlineData("a ] b", 2, ']')]
    public void Compile_UnexpectedClosingDelimiter_ReportsTheClosingToken_Test(string text, int offset, char closer)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.SyntaxError);
        Assert.Equal(new SourceSpan(offset, 1), diagnostic.Span);
        Assert.Equal($"Unexpected closing '{closer}' with no matching opener.", diagnostic.Message);
    }

    /// <summary>Braces and brackets inside a string literal are plain text, never delimiters.</summary>
    [Fact]
    public void Compile_DelimitersInsideAStringLiteral_AreNotGroupingDelimiters_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .Add(
                    new PredicateSchema(
                        "named",
                        "Named",
                        "Has a name.",
                        [new PredicateArgumentSchema("value", "The name.", LiteralKind.String)]
                    ),
                    (_, _, _) => ValueTask.FromResult(TruthValue.True)
                )
                .Build()
        );

        CompilationResult<RuleTestContext> result = compiler.Compile("{named(value: \"}) ] [ {\")}");

        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
    }
}
