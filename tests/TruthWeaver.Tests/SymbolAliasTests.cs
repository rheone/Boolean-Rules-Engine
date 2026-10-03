namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Symbol notation (<c>&amp;&amp; || ! ∧ ∨ ¬ ⊕</c>) is accepted on input for the existing operators and
/// compiles to exactly the same tree as the named operator (ADR-0005 decision 2). Notation is a style
/// choice, never a semantic one.
/// </summary>
public sealed class SymbolAliasTests
{
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .Build()
    );

    /// <summary>Each symbol compiles to a tree whose canonical text equals the named operator's.</summary>
    [Theory]
    [InlineData("a && b", "a AND b")]
    [InlineData("a ∧ b", "a AND b")]
    [InlineData("a || b", "a OR b")]
    [InlineData("a ∨ b", "a OR b")]
    [InlineData("!a", "NOT a")]
    [InlineData("¬a", "NOT a")]
    [InlineData("a ⊕ b", "a XOR b")]
    [InlineData("a && b && c", "a AND b AND c")]
    [InlineData("a || b || c", "a OR b OR c")]
    public void Compile_SymbolOperator_ProducesSameCanonicalTextAsNamedOperator_Test(string symbolic, string named)
    {
        string fromSymbol = Compiler.Compile(symbolic).CompiledRule!.CanonicalText;
        string fromNamed = Compiler.Compile(named).CompiledRule!.CanonicalText;

        Assert.Equal(fromNamed, fromSymbol);
    }

    /// <summary>The canonical printer stays word-only: no symbol ever appears in the printed text.</summary>
    [Theory]
    [InlineData("a && b", "a AND b")]
    [InlineData("a ∨ !b", "a OR NOT b")]
    [InlineData("¬(a ∧ b) ⊕ c", "(NOT (a AND b) XOR c)")]
    public void CanonicalText_SymbolInput_IsPrintedWithNamedOperatorsOnly_Test(string symbolic, string expected)
    {
        string text = Compiler.Compile(symbolic).CompiledRule!.CanonicalText;

        Assert.Equal(expected, text);
    }

    /// <summary>
    /// Symbols and words mix freely and keep the NOT &gt; AND &gt; OR precedence: the symbolic text
    /// evaluates identically to its explicitly parenthesised named form for every K3 input.
    /// </summary>
    [Theory]
    [InlineData("a || b && c", "a OR (b AND c)")]
    [InlineData("a ∨ b ∧ c", "a OR (b AND c)")]
    [InlineData("!a && b", "(NOT a) AND b")]
    [InlineData("¬a ∧ b", "(NOT a) AND b")]
    [InlineData("a && b OR c", "(a AND b) OR c")]
    [InlineData("a OR b && c", "a OR (b AND c)")]
    [InlineData("!!a", "NOT NOT a")]
    public async Task Evaluate_SymbolOperators_KeepNamedOperatorPrecedence_Test(string symbolic, string explicitNamed)
    {
        K3Rule fromSymbol = K3Rule.TryCreate(symbolic, 3)!;
        K3Rule fromNamed = K3Rule.TryCreate(explicitNamed, 3)!;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(3))
        {
            Decision expected = await fromNamed.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
            Decision actual = await fromSymbol.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(expected.Result, actual.Result);
        }
    }

    /// <summary>A symbolic XOR mixed with AND/OR without parentheses is rejected like the word XOR.</summary>
    [Theory]
    [InlineData("a ⊕ b && c")]
    [InlineData("a || b ⊕ c")]
    public void Compile_SymbolXorMixedWithAndOrWithoutParentheses_ReportsAmbiguousMixing_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.AmbiguousOperatorMixing);
    }

    /// <summary>A lone '&amp;' or '|' is not an operator and is reported as a syntax error.</summary>
    [Theory]
    [InlineData("a & b")]
    [InlineData("a | b")]
    public void Compile_SingleAmpersandOrPipe_ReportsSyntaxError_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.SyntaxError);
    }

    /// <summary>Symbolic AND/OR/NOT evaluate exactly like the K3 oracle over every input.</summary>
    [Fact]
    public async Task Evaluate_SymbolicAndOrNot_MatchOracleOverAllInputs_Test()
    {
        K3Rule and = K3Rule.TryCreate("a && b", 2)!;
        K3Rule or = K3Rule.TryCreate("a ∨ b", 2)!;
        K3Rule not = K3Rule.TryCreate("!a", 1)!;
        K3Rule notSymbol = K3Rule.TryCreate("¬a", 1)!;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(2))
        {
            Assert.Equal(
                K3Oracle.And(assignment),
                (await and.EvaluateAsync(assignment, TestContext.Current.CancellationToken)).Result
            );
            Assert.Equal(
                K3Oracle.Or(assignment),
                (await or.EvaluateAsync(assignment, TestContext.Current.CancellationToken)).Result
            );
        }

        foreach (TruthValue[] assignment in K3Oracle.Assignments(1))
        {
            Assert.Equal(
                K3Oracle.Not(assignment[0]),
                (await not.EvaluateAsync(assignment, TestContext.Current.CancellationToken)).Result
            );
            Assert.Equal(
                K3Oracle.Not(assignment[0]),
                (await notSymbol.EvaluateAsync(assignment, TestContext.Current.CancellationToken)).Result
            );
        }
    }

    /// <summary>Symbolic XOR evaluates exactly like the K3 oracle over every input.</summary>
    [Fact]
    public async Task Evaluate_SymbolicXor_MatchesOracleOverAllInputs_Test()
    {
        K3Rule xor = K3Rule.TryCreate("a ⊕ b", 2)!;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(2))
        {
            Decision decision = await xor.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(K3Oracle.Xor(assignment[0], assignment[1]), decision.Result);
        }
    }
}
