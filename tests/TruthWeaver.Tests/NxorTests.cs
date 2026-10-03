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
/// <c>NXOR(a, b, ...)</c> is n-ary parity (ADR-0005 decision 4): a first-class function-call node that is
/// <c>Unknown</c> whenever any operand is <c>Unknown</c>. Binary <c>XOR</c> with three or more operands stays a
/// compile error whose message points at <c>NXOR</c>; <c>ExactlyOne</c> is unchanged and is a different operation.
/// </summary>
public sealed class NxorTests
{
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .Build()
    );

    /// <summary>NXOR over 2..4 operands, in upper, lower and mixed case, matches the oracle's parity for every assignment.</summary>
    [Theory]
    [InlineData("NXOR")]
    [InlineData("nxor")]
    [InlineData("Nxor")]
    public async Task Evaluate_NxorOverAllAssignments_MatchesOracle_Test(string keyword)
    {
        for (int arity = 2; arity <= 4; arity++)
        {
            string names = string.Join(", ", Enumerable.Range(0, arity).Select(i => (char)('a' + i)));
            K3Rule rule = K3Rule.TryCreate($"{keyword}({names})", arity)!;

            foreach (TruthValue[] assignment in K3Oracle.Assignments(arity))
            {
                Decision actual = await rule.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

                Assert.Equal(K3Oracle.Nxor(assignment), actual.Result);
            }
        }
    }

    /// <summary>Parity, not exactly-one: three True operands make NXOR True but ExactlyOne False.</summary>
    [Fact]
    public async Task Evaluate_NxorWithThreeTrueOperands_DiffersFromExactlyOne_Test()
    {
        K3Rule nxor = K3Rule.TryCreate("NXOR(a, b, c)", 3)!;
        K3Rule exactlyOne = K3Rule.TryCreate("ExactlyOne(a, b, c)", 3)!;
        TruthValue[] allTrue = [TruthValue.True, TruthValue.True, TruthValue.True];

        Decision parity = await nxor.EvaluateAsync(allTrue, TestContext.Current.CancellationToken);
        Decision single = await exactlyOne.EvaluateAsync(allTrue, TestContext.Current.CancellationToken);

        Assert.Equal(TruthValue.True, parity.Result);
        Assert.Equal(TruthValue.False, single.Result);
    }

    /// <summary>A known True next to an Unknown does not decide parity the way it decides ExactlyOne's "two Trues" case.</summary>
    [Fact]
    public async Task Evaluate_NxorWithTwoTrueAndAnUnknown_IsUnknownWhereExactlyOneIsFalse_Test()
    {
        K3Rule nxor = K3Rule.TryCreate("NXOR(a, b, c)", 3)!;
        K3Rule exactlyOne = K3Rule.TryCreate("ExactlyOne(a, b, c)", 3)!;
        TruthValue[] values = [TruthValue.True, TruthValue.True, TruthValue.Unknown];

        Decision parity = await nxor.EvaluateAsync(values, TestContext.Current.CancellationToken);
        Decision single = await exactlyOne.EvaluateAsync(values, TestContext.Current.CancellationToken);

        Assert.Equal(TruthValue.Unknown, parity.Result);
        Assert.Equal(TruthValue.False, single.Result);
    }

    /// <summary>Every spelling prints as the canonical function-call form.</summary>
    [Theory]
    [InlineData("NXOR(a, b, c)")]
    [InlineData("nxor(a,b,c)")]
    [InlineData("Nxor( a , b , c )")]
    public void Compile_AnyNxorSpelling_ProducesTheSameCanonicalText_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.True(result.Succeeded);
        Assert.Equal("NXOR(a, b, c)", result.CompiledRule!.CanonicalText);
    }

    /// <summary>A function-call form has no precedence, so it combines with infix operators without parentheses.</summary>
    [Theory]
    [InlineData("NXOR(a, b) AND c")]
    [InlineData("a OR NXOR(b, c)")]
    [InlineData("NXOR(a, b) XOR c")]
    [InlineData("NOT NXOR(a, b, c)")]
    [InlineData("NXOR(a AND b, c XOR a, NXOR(a, b))")]
    public void Compile_NxorNextToOtherOperators_NeedsNoParentheses_Test(string text)
    {
        Assert.True(Compiler.Compile(text).Succeeded);
    }

    /// <summary>The operator name is reserved so a predicate cannot shadow it.</summary>
    [Theory]
    [InlineData("nxor")]
    [InlineData("NXOR")]
    public void IsReservedWord_Nxor_IsReserved_Test(string word)
    {
        Assert.True(DslParser.IsReservedWord(word));
    }

    /// <summary>NXOR needs at least two operands, like AND, OR and ExactlyOne.</summary>
    [Theory]
    [InlineData("NXOR(a)")]
    [InlineData("NXOR()")]
    public void Compile_NxorWithFewerThanTwoOperands_ReportsMalformedTree_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MalformedTree);
    }

    /// <summary>Binary XOR with three operands is still the arity error, now pointing at NXOR for parity.</summary>
    [Fact]
    public void Compile_XorChainOfThree_ReportsArityViolationNamingNxor_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile("a XOR b XOR c");

        Assert.False(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.InfixArityViolation, diagnostic.Code);
        Assert.Contains("NXOR", diagnostic.Message, StringComparison.Ordinal);
    }

    /// <summary>The hint also names ExactlyOne so an author who wanted "exactly one" is not misled into parity.</summary>
    [Fact]
    public void Compile_XorChainOfThree_StillMentionsExactlyOne_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile("a XOR b XOR c");

        Assert.Contains("ExactlyOne", Assert.Single(result.Diagnostics).Message, StringComparison.Ordinal);
    }

    /// <summary>A JSON XOR with three operands gets the same hint.</summary>
    [Fact]
    public void CompileJson_XorWithThreeOperands_ReportsArityViolationNamingNxor_Test()
    {
        const string Json = """{"op": "xor", "operands": [{"predicate": "a"}, {"predicate": "b"}, {"predicate": "c"}]}""";

        CompilationResult<RuleTestContext> result = Compiler.CompileJson(Json);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.InfixArityViolation, diagnostic.Code);
        Assert.Contains("NXOR", diagnostic.Message, StringComparison.Ordinal);
    }

    /// <summary>JSON uses the <c>nxor</c> op, round-trips, and reads the op name in any letter case.</summary>
    [Fact]
    public void PrintJson_Nxor_RoundTripsWithTheOpName_Test()
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile("NXOR(a, b, c)").CompiledRule!;

        string json = original.PrintJson();
        CompiledRule<RuleTestContext> reparsed = Compiler
            .CompileJson(json.Replace("nxor", "NXOR", StringComparison.Ordinal))
            .CompiledRule!;

        Assert.Contains("\"op\":\"nxor\"", json.Replace(" ", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>YAML uses the same node shape and round-trips.</summary>
    [Fact]
    public void PrintYaml_Nxor_RoundTripsWithTheOpName_Test()
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile("NXOR(a, b, c)").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<RuleTestContext> reparsed = Compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Contains("op: nxor", yaml, StringComparison.Ordinal);
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>The builder produces the same rule as the DSL.</summary>
    [Fact]
    public void Nxor_Builder_CompilesToTheSameCanonicalTextAsDsl_Test()
    {
        CompiledRule<RuleTestContext> rule = RuleBuilder
            .Nxor(RuleBuilder.Predicate("a"), RuleBuilder.Not(RuleBuilder.Predicate("b")), RuleBuilder.Predicate("c"))
            .Compile(Compiler)
            .CompiledRule!;

        Assert.Equal("NXOR(a, NOT b, c)", rule.CanonicalText);
    }

    /// <summary>The evaluated tree labels the node NXOR and keeps operand order.</summary>
    [Fact]
    public async Task EvaluateAsync_Nxor_LabelsTheNodeAndKeepsOperandOrder_Test()
    {
        K3Rule rule = K3Rule.TryCreate("NXOR(a, b, c)", 3)!;

        Decision decision = await rule.EvaluateAsync(
            [TruthValue.True, TruthValue.False, TruthValue.True],
            TestContext.Current.CancellationToken
        );

        Assert.Equal("NXOR", decision.EvaluatedTree!.NodeDescription);
        Assert.Equal(["a", "b", "c"], decision.EvaluatedTree.Children.Select(c => c.NodeDescription));
    }

    /// <summary>The operator description explains parity and the Unknown rule.</summary>
    [Fact]
    public void Describe_Nxor_ExplainsParityAndUnknown_Test()
    {
        RuleDescription description = Compiler.Compile("NXOR(a, b, c)").CompiledRule!.Describe();

        Assert.Equal("NXOR", description.Label);
        Assert.Contains("odd number", description.Description, StringComparison.Ordinal);
        Assert.Contains("Unknown", description.Description, StringComparison.Ordinal);
        Assert.Equal(["a", "b", "c"], description.Operands.Select(o => o.Label));
    }

    /// <summary>Tree renderings keep the function-call word in every operator style (NXOR has no symbol).</summary>
    [Theory]
    [InlineData(OperatorStyle.Word)]
    [InlineData(OperatorStyle.Symbolic)]
    [InlineData(OperatorStyle.CStyle)]
    public void Print_Nxor_KeepsTheWordInEveryStyle_Test(OperatorStyle style)
    {
        RuleDescription tree = Compiler.Compile("NXOR(a, b, c)").CompiledRule!.Describe();

        Assert.Contains("NXOR", PlainTextTreePrinter.Print(tree, style), StringComparison.Ordinal);
        Assert.Contains("NXOR", MermaidTreePrinter.Print(tree, style), StringComparison.Ordinal);
    }
}
