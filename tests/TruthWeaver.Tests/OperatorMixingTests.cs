namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// The no-mixing rule (ADR-0005 decision 8): every infix operator other than <c>NOT</c>/<c>AND</c>/<c>OR</c>
/// must not be mixed with another infix operator at the same nesting level without parentheses, while
/// <c>NOT &gt; AND &gt; OR</c> keeps parsing without them.
/// </summary>
public sealed class OperatorMixingTests
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

    /// <summary>
    /// Mixing two different infix operators points at the second operator (the one that makes the level
    /// ambiguous); an infix expression that is a bare operand of AND/OR is reported at its own span.
    /// </summary>
    [Theory]
    [InlineData("a XOR b XNOR c", 8, 4)]
    [InlineData("a XNOR b XOR c", 9, 3)]
    [InlineData("a XOR b XNOR c XOR d", 8, 4)]
    [InlineData("a XNOR b ⊕ c", 9, 1)]
    [InlineData("a XOR b AND c", 0, 7)]
    [InlineData("a AND b XOR c", 6, 7)]
    [InlineData("a OR b XNOR c", 5, 8)]
    [InlineData("a || b ⊕ c", 5, 5)]
    public void Compile_AmbiguousMixing_ReportsAtThePreciseOffendingSpan_Test(string text, int start, int length)
    {
        Diagnostic diagnostic = SingleMixingDiagnostic(text);

        Assert.Equal(new SourceSpan(start, length), diagnostic.Span);
    }

    /// <summary>Every mixing diagnostic tells the author to add parentheses.</summary>
    [Theory]
    [InlineData("a XOR b XNOR c")]
    [InlineData("a XOR b AND c")]
    [InlineData("a OR b XNOR c")]
    [InlineData("a ⊕ b || c")]
    public void Compile_AmbiguousMixing_SuggestsAddingParentheses_Test(string text)
    {
        Diagnostic diagnostic = SingleMixingDiagnostic(text);

        Assert.Contains("Add parentheses", diagnostic.Message);
    }

    /// <summary>The message names the operators involved so the author knows which pair is ambiguous.</summary>
    [Theory]
    [InlineData("a XOR b XNOR c", "XOR", "EQUIVALENT")]
    [InlineData("a XNOR b ⊕ c", "EQUIVALENT", "XOR")]
    [InlineData("a XNOR b AND c", "EQUIVALENT", "AND/OR")]
    public void Compile_AmbiguousMixing_NamesTheOperatorsInvolved_Test(string text, string first, string second)
    {
        Diagnostic diagnostic = SingleMixingDiagnostic(text);

        Assert.Contains($"Mixing {first} with {second}", diagnostic.Message);
    }

    /// <summary>Parentheses (and function-call operand lists, which are their own level) remove the ambiguity.</summary>
    [Theory]
    [InlineData("(a XOR b) XNOR c")]
    [InlineData("a XOR (b XNOR c)")]
    [InlineData("(a XOR b) AND c")]
    [InlineData("a OR (b XNOR c)")]
    [InlineData("ExactlyOne(a XOR b, c)")]
    [InlineData("AtLeast(1, a XNOR b, c AND d)")]
    public void Compile_ExplicitGrouping_DoesNotReportMixing_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.DoesNotContain(result.Diagnostics, d => d.Code == DiagnosticCodes.AmbiguousOperatorMixing);
        Assert.True(result.Succeeded);
    }

    /// <summary>NOT &gt; AND &gt; OR still parses without parentheses and evaluates per that precedence, over every K3 input.</summary>
    [Theory]
    [InlineData("NOT a AND b OR c AND d", "((NOT a) AND b) OR (c AND d)")]
    [InlineData("a OR b AND NOT c", "a OR (b AND (NOT c))")]
    [InlineData("a AND b OR c", "(a AND b) OR c")]
    public async Task Evaluate_NotAndOrPrecedence_NeedsNoParentheses_Test(string bare, string explicitGrouping)
    {
        K3Rule fromBare = K3Rule.TryCreate(bare, 4)!;
        K3Rule fromExplicit = K3Rule.TryCreate(explicitGrouping, 4)!;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(4))
        {
            Decision expected = await fromExplicit.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
            Decision actual = await fromBare.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(expected.Result, actual.Result);
        }
    }

    /// <summary>NOT binds tighter than a single infix operator, so a negated operand needs no parentheses.</summary>
    [Fact]
    public void Compile_NotOperandOfInfixOperator_NeedsNoParentheses_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile("NOT a XOR !b");

        Assert.True(result.Succeeded);
    }

    private static Diagnostic SingleMixingDiagnostic(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);
        Assert.False(result.Succeeded);
        return Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.AmbiguousOperatorMixing);
    }
}
