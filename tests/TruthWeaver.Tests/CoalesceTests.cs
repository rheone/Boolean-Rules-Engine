namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Parsing;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// <c>COALESCE(a, b, ...)</c> and the infix <c>??</c> replace only <c>Unknown</c> with the next operand
/// (ADR-0005 decisions 3a and 8): <c>True</c> and <c>False</c> pass through unchanged.
/// </summary>
public sealed class CoalesceTests
{
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .Build()
    );

    /// <summary>The function-call and infix forms over 2..4 operands match the oracle for every assignment.</summary>
    [Theory]
    [InlineData("COALESCE")]
    [InlineData("coalesce")]
    [InlineData("Coalesce")]
    [InlineData("??")]
    public async Task Evaluate_OverAllAssignments_MatchesOracle_Test(string spelling)
    {
        for (int arity = 2; arity <= 4; arity++)
        {
            string[] names = [.. Enumerable.Range(0, arity).Select(i => ((char)('a' + i)).ToString())];
            string text = spelling == "??" ? string.Join(" ?? ", names) : $"{spelling}({string.Join(", ", names)})";
            K3Rule rule = K3Rule.TryCreate(text, arity)!;

            foreach (TruthValue[] assignment in K3Oracle.Assignments(arity))
            {
                Decision actual = await rule.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

                Assert.Equal(K3Oracle.Coalesce(assignment), actual.Result);
            }
        }
    }

    /// <summary>Spot-check: Unknown is replaced by the next operand, True and False pass through, all-Unknown stays Unknown.</summary>
    [Theory]
    [InlineData(TruthValue.Unknown, TruthValue.True, TruthValue.True)]
    [InlineData(TruthValue.Unknown, TruthValue.False, TruthValue.False)]
    [InlineData(TruthValue.Unknown, TruthValue.Unknown, TruthValue.Unknown)]
    [InlineData(TruthValue.True, TruthValue.False, TruthValue.True)]
    [InlineData(TruthValue.False, TruthValue.True, TruthValue.False)]
    public async Task Evaluate_Binary_ReplacesOnlyUnknown_Test(TruthValue first, TruthValue second, TruthValue expected)
    {
        K3Rule rule = K3Rule.TryCreate("COALESCE(a, b)", 2)!;

        Decision decision = await rule.EvaluateAsync([first, second], TestContext.Current.CancellationToken);

        Assert.Equal(expected, decision.Result);
    }

    /// <summary>Operands after the first known value are skipped in the evaluated tree, as AND/OR skip theirs.</summary>
    [Fact]
    public async Task EvaluateAsync_AfterTheFirstKnownOperand_MarksTheRestSkipped_Test()
    {
        K3Rule rule = K3Rule.TryCreate("COALESCE(a, b, c)", 3)!;

        Decision decision = await rule.EvaluateAsync(
            [TruthValue.Unknown, TruthValue.False, TruthValue.True],
            TestContext.Current.CancellationToken
        );

        Assert.Equal("COALESCE", decision.EvaluatedTree!.NodeDescription);
        Assert.Equal([false, false, true], decision.EvaluatedTree.Children.Select(c => c.NotEvaluated));
        Assert.Equal(TruthValue.False, decision.Result);
    }

    /// <summary>Every operand is evaluated while no known value has been found.</summary>
    [Fact]
    public async Task EvaluateAsync_WhileEveryOperandIsUnknown_EvaluatesAll_Test()
    {
        K3Rule rule = K3Rule.TryCreate("COALESCE(a, b, c)", 3)!;

        Decision decision = await rule.EvaluateAsync(
            [TruthValue.Unknown, TruthValue.Unknown, TruthValue.Unknown],
            TestContext.Current.CancellationToken
        );

        Assert.All(decision.EvaluatedTree!.Children, c => Assert.False(c.NotEvaluated));
    }

    /// <summary>Both spellings print as the canonical function-call form.</summary>
    [Theory]
    [InlineData("COALESCE(a, b)", "COALESCE(a, b)")]
    [InlineData("coalesce(a,b,c)", "COALESCE(a, b, c)")]
    [InlineData("a ?? b", "COALESCE(a, b)")]
    [InlineData("a ?? b ?? c", "COALESCE(a, b, c)")]
    [InlineData("NOT a ?? b", "COALESCE(NOT a, b)")]
    [InlineData("(a AND b) ?? c", "COALESCE(a AND b, c)")]
    public void Compile_AnySpelling_ProducesTheCanonicalText_Test(string text, string expected)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.True(result.Succeeded);
        Assert.Equal(expected, result.CompiledRule!.CanonicalText);
    }

    /// <summary>A chain of the same infix operator is accepted because coalescing is associative (it builds one n-ary node).</summary>
    [Fact]
    public void Compile_ChainedInfix_BuildsOneNaryNode_Test()
    {
        CompiledRule<RuleTestContext> chained = Compiler.Compile("a ?? b ?? c").CompiledRule!;
        CompiledRule<RuleTestContext> grouped = Compiler.Compile("(a ?? b) ?? c").CompiledRule!;

        Assert.Equal("COALESCE(a, b, c)", chained.CanonicalText);
        Assert.Equal("COALESCE(COALESCE(a, b), c)", grouped.CanonicalText);
    }

    /// <summary>The infix form follows the no-mixing rule: it cannot share a level with AND/OR or another infix operator.</summary>
    [Theory]
    [InlineData("a ?? b AND c")]
    [InlineData("a AND b ?? c")]
    [InlineData("a OR b ?? c")]
    [InlineData("a ?? b XOR c")]
    [InlineData("a XOR b ?? c")]
    [InlineData("a ?? b IMPLIES c")]
    public void Compile_InfixMixedWithoutParentheses_ReportsAmbiguousMixing_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.AmbiguousOperatorMixing);
    }

    /// <summary>With parentheses the infix form combines with every other operator.</summary>
    [Theory]
    [InlineData("(a ?? b) AND c")]
    [InlineData("a OR (b ?? c)")]
    [InlineData("(a ?? b) XOR c")]
    [InlineData("COALESCE(a, b) AND c")]
    [InlineData("NOT COALESCE(a, b)")]
    public void Compile_WithParenthesesOrCallForm_Succeeds_Test(string text)
    {
        Assert.True(Compiler.Compile(text).Succeeded);
    }

    /// <summary>COALESCE is reserved so a predicate cannot shadow it.</summary>
    [Theory]
    [InlineData("coalesce")]
    [InlineData("COALESCE")]
    public void IsReservedWord_Coalesce_IsReserved_Test(string word)
    {
        Assert.True(DslParser.IsReservedWord(word));
    }

    /// <summary>The word COALESCE is a function call only; it is not an infix operator (only the symbol ?? is).</summary>
    [Fact]
    public void Compile_CoalesceWordInInfixPosition_IsRejected_Test()
    {
        Assert.False(Compiler.Compile("a COALESCE b").Succeeded);
    }

    /// <summary>COALESCE needs at least two operands, like AND/OR.</summary>
    [Theory]
    [InlineData("COALESCE(a)")]
    [InlineData("COALESCE()")]
    public void Compile_WithFewerThanTwoOperands_ReportsMalformedTree_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MalformedTree);
    }

    /// <summary>A dangling infix operator is a syntax error.</summary>
    [Fact]
    public void Compile_DanglingInfix_ReportsSyntaxError_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile("a ??");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.SyntaxError);
    }

    /// <summary>JSON uses the 'coalesce' op, round-trips, and reads the op name in any letter case.</summary>
    [Fact]
    public void PrintJson_Coalesce_RoundTripsWithTheOpName_Test()
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile("COALESCE(a, b, c)").CompiledRule!;

        string json = original.PrintJson();
        CompiledRule<RuleTestContext> reparsed = Compiler
            .CompileJson(json.Replace("coalesce", "COALESCE", StringComparison.Ordinal))
            .CompiledRule!;

        Assert.Contains(
            "\"op\":\"coalesce\"",
            json.Replace(" ", string.Empty, StringComparison.Ordinal),
            StringComparison.Ordinal
        );
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>YAML uses the same node shape and round-trips.</summary>
    [Fact]
    public void PrintYaml_Coalesce_RoundTripsWithTheOpName_Test()
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile("COALESCE(a, b, c)").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<RuleTestContext> reparsed = Compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Contains("op: coalesce", yaml, StringComparison.Ordinal);
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>The builder produces the same rule as the DSL.</summary>
    [Fact]
    public void Coalesce_Builder_CompilesToTheSameCanonicalTextAsDsl_Test()
    {
        RuleBuilder builder = RuleBuilder.Coalesce(
            RuleBuilder.Predicate("a"),
            RuleBuilder.Not(RuleBuilder.Predicate("b")),
            RuleBuilder.Predicate("c")
        );

        CompilationResult<RuleTestContext> result = builder.Compile(Compiler);

        Assert.True(result.Succeeded);
        Assert.Equal("COALESCE(a, NOT b, c)", result.CompiledRule!.CanonicalText);
    }

    /// <summary>The operator description states that only Unknown is replaced.</summary>
    [Fact]
    public void Describe_Coalesce_ExplainsThatOnlyUnknownIsReplaced_Test()
    {
        RuleDescription description = Compiler.Compile("COALESCE(a, b)").CompiledRule!.Describe();

        Assert.Equal("COALESCE", description.Label);
        Assert.Contains("Unknown", description.Description, StringComparison.Ordinal);
        Assert.Equal(["a", "b"], description.Operands.Select(o => o.Label));
    }

    /// <summary>Symbolic and C-style tree renderings spell the node <c>??</c>; the word style keeps COALESCE.</summary>
    [Theory]
    [InlineData(OperatorStyle.Word, "COALESCE")]
    [InlineData(OperatorStyle.Symbolic, "??")]
    [InlineData(OperatorStyle.CStyle, "??")]
    public void Print_Coalesce_UsesTheStyleSpelling_Test(OperatorStyle style, string expected)
    {
        RuleDescription tree = Compiler.Compile("COALESCE(a, b)").CompiledRule!.Describe();

        Assert.Contains(expected, PlainTextTreePrinter.Print(tree, style), StringComparison.Ordinal);
        Assert.Contains(expected, MermaidTreePrinter.Print(tree, style), StringComparison.Ordinal);
    }
}
