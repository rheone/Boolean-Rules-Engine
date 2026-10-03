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
/// <c>If(condition, whenTrue, whenFalse)</c> and the ternary <c>condition ? whenTrue : whenFalse</c> are a K3-aware
/// conditional (ADR-0005 decisions 3 and 8): an <c>Unknown</c> condition never guesses a branch.
/// </summary>
public sealed class IfTests
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

    /// <summary>The function-call and ternary forms match the oracle for every assignment of condition and branches.</summary>
    [Theory]
    [InlineData("If(a, b, c)")]
    [InlineData("if(a, b, c)")]
    [InlineData("IF(a,b,c)")]
    [InlineData("a ? b : c")]
    public async Task Evaluate_OverAllAssignments_MatchesOracle_Test(string text)
    {
        K3Rule rule = K3Rule.TryCreate(text, 3)!;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(3))
        {
            Decision actual = await rule.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(K3Oracle.If(assignment[0], assignment[1], assignment[2]), actual.Result);
        }
    }

    /// <summary>A definite condition picks its branch outright, whatever the other branch holds.</summary>
    [Theory]
    [InlineData(TruthValue.True, TruthValue.False, TruthValue.True, TruthValue.False)]
    [InlineData(TruthValue.True, TruthValue.Unknown, TruthValue.True, TruthValue.Unknown)]
    [InlineData(TruthValue.False, TruthValue.True, TruthValue.Unknown, TruthValue.Unknown)]
    [InlineData(TruthValue.False, TruthValue.True, TruthValue.False, TruthValue.False)]
    public async Task Evaluate_DefiniteCondition_PicksItsBranch_Test(
        TruthValue condition,
        TruthValue whenTrue,
        TruthValue whenFalse,
        TruthValue expected
    )
    {
        K3Rule rule = K3Rule.TryCreate("If(a, b, c)", 3)!;

        Decision decision = await rule.EvaluateAsync([condition, whenTrue, whenFalse], TestContext.Current.CancellationToken);

        Assert.Equal(expected, decision.Result);
    }

    /// <summary>An Unknown condition yields the branch value only when both branches agree on a definite value.</summary>
    [Theory]
    [InlineData(TruthValue.True, TruthValue.True, TruthValue.True)]
    [InlineData(TruthValue.False, TruthValue.False, TruthValue.False)]
    [InlineData(TruthValue.True, TruthValue.False, TruthValue.Unknown)]
    [InlineData(TruthValue.False, TruthValue.True, TruthValue.Unknown)]
    [InlineData(TruthValue.Unknown, TruthValue.Unknown, TruthValue.Unknown)]
    [InlineData(TruthValue.True, TruthValue.Unknown, TruthValue.Unknown)]
    public async Task Evaluate_UnknownCondition_DoesNotGuessABranch_Test(
        TruthValue whenTrue,
        TruthValue whenFalse,
        TruthValue expected
    )
    {
        K3Rule rule = K3Rule.TryCreate("a ? b : c", 3)!;

        Decision decision = await rule.EvaluateAsync(
            [TruthValue.Unknown, whenTrue, whenFalse],
            TestContext.Current.CancellationToken
        );

        Assert.Equal(expected, decision.Result);
    }

    /// <summary>A True condition skips the whenFalse branch in the evaluated tree.</summary>
    [Fact]
    public async Task EvaluateAsync_TrueCondition_MarksWhenFalseSkipped_Test()
    {
        K3Rule rule = K3Rule.TryCreate("If(a, b, c)", 3)!;

        Decision decision = await rule.EvaluateAsync(
            [TruthValue.True, TruthValue.False, TruthValue.True],
            TestContext.Current.CancellationToken
        );

        Assert.Equal("If", decision.EvaluatedTree!.NodeDescription);
        Assert.Equal([false, false, true], decision.EvaluatedTree.Children.Select(c => c.NotEvaluated));
    }

    /// <summary>A False condition skips the whenTrue branch in the evaluated tree.</summary>
    [Fact]
    public async Task EvaluateAsync_FalseCondition_MarksWhenTrueSkipped_Test()
    {
        K3Rule rule = K3Rule.TryCreate("If(a, b, c)", 3)!;

        Decision decision = await rule.EvaluateAsync(
            [TruthValue.False, TruthValue.False, TruthValue.True],
            TestContext.Current.CancellationToken
        );

        Assert.Equal([false, true, false], decision.EvaluatedTree!.Children.Select(c => c.NotEvaluated));
    }

    /// <summary>An Unknown condition needs both branches to decide, so neither is skipped.</summary>
    [Fact]
    public async Task EvaluateAsync_UnknownCondition_EvaluatesBothBranches_Test()
    {
        K3Rule rule = K3Rule.TryCreate("If(a, b, c)", 3)!;

        Decision decision = await rule.EvaluateAsync(
            [TruthValue.Unknown, TruthValue.True, TruthValue.True],
            TestContext.Current.CancellationToken
        );

        Assert.All(decision.EvaluatedTree!.Children, c => Assert.False(c.NotEvaluated));
    }

    /// <summary>The skipped branch's predicate is never invoked, so a definite condition avoids its cost.</summary>
    [Fact]
    public async Task EvaluateAsync_TrueCondition_DoesNotInvokeTheSkippedBranchPredicate_Test()
    {
        List<string> invocationLog = [];
        RuleCompiler<RuleTestContext> counting = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddCountingConstant("a", true, invocationLog)
                .AddCountingConstant("b", true, invocationLog)
                .AddCountingConstant("c", false, invocationLog)
                .Build()
        );
        CompiledRule<RuleTestContext> rule = counting.Compile("If(a, b, c)").CompiledRule!;

        await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(["a", "b"], invocationLog);
    }

    /// <summary>Exhaustive mode evaluates the skipped branch too, like AND/OR.</summary>
    [Fact]
    public async Task EvaluateAsync_ExhaustiveMode_EvaluatesBothBranches_Test()
    {
        List<string> invocationLog = [];
        RuleCompiler<RuleTestContext> counting = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddCountingConstant("a", true, invocationLog)
                .AddCountingConstant("b", true, invocationLog)
                .AddCountingConstant("c", false, invocationLog)
                .Build()
        );
        CompiledRule<RuleTestContext> rule = counting.Compile("If(a, b, c)").CompiledRule!;

        await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            new EvaluationOptions(Mode: EvaluationMode.Exhaustive),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(["a", "b", "c"], invocationLog);
    }

    /// <summary>Both spellings, alone or nested in other operators, print as the canonical function-call form.</summary>
    [Theory]
    [InlineData("If(a, b, c)", "If(a, b, c)")]
    [InlineData("IF(a,b,c)", "If(a, b, c)")]
    [InlineData("a ? b : c", "If(a, b, c)")]
    [InlineData("NOT a ? b : c", "If(NOT a, b, c)")]
    [InlineData("(a AND b) ? c : d", "If(a AND b, c, d)")]
    [InlineData("a ? (b OR c) : d", "If(a, b OR c, d)")]
    [InlineData("a ? b : (c XOR d)", "If(a, b, (c XOR d))")]
    [InlineData("(a ? b : c) AND d", "If(a, b, c) AND d")]
    [InlineData("NOT (a ? b : c)", "NOT If(a, b, c)")]
    [InlineData("a ? (b ? c : d) : a", "If(a, If(b, c, d), a)")]
    [InlineData("If(a ? b : c, d, a)", "If(If(a, b, c), d, a)")]
    [InlineData("ANY(a ? b : c, d)", "ANY(If(a, b, c), d)")]
    public void Compile_AnySpelling_ProducesTheCanonicalText_Test(string text, string expected)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.True(result.Succeeded);
        Assert.Equal(expected, result.CompiledRule!.CanonicalText);
    }

    /// <summary>The ternary follows the no-mixing rule: no part may be a bare AND/OR chain or other infix expression.</summary>
    [Theory]
    [InlineData("a AND b ? c : d")]
    [InlineData("a OR b ? c : d")]
    [InlineData("a ? b AND c : d")]
    [InlineData("a ? b : c OR d")]
    [InlineData("a XOR b ? c : d")]
    [InlineData("a ? b XOR c : d")]
    [InlineData("a ? b : c IMPLIES d")]
    [InlineData("a ?? b ? c : d")]
    [InlineData("a ? b : c ?? d")]
    [InlineData("a ? b : c ? d : a")]
    [InlineData("a ? b ? c : d : a")]
    public void Compile_TernaryMixedWithoutParentheses_ReportsAmbiguousMixing_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.AmbiguousOperatorMixing);
    }

    /// <summary>With parentheses the ternary combines with every other operator.</summary>
    [Theory]
    [InlineData("(a ? b : c) XOR d")]
    [InlineData("a ? (b XOR c) : d")]
    [InlineData("(a AND b) ? (c OR d) : (a ?? b)")]
    [InlineData("If(a AND b, c OR d, a XOR b)")]
    public void Compile_WithParentheses_Succeeds_Test(string text)
    {
        Assert.True(Compiler.Compile(text).Succeeded);
    }

    /// <summary>A lone '?' is a ternary token; an incomplete ternary is a syntax error, not a crash.</summary>
    [Theory]
    [InlineData("a ? b")]
    [InlineData("a ? b :")]
    [InlineData("a ? : c")]
    [InlineData("? b : c")]
    [InlineData("a : b")]
    public void Compile_IncompleteTernary_ReportsSyntaxError_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.SyntaxError);
    }

    /// <summary>If is reserved so a predicate cannot shadow it.</summary>
    [Theory]
    [InlineData("if")]
    [InlineData("IF")]
    [InlineData("If")]
    public void IsReservedWord_If_IsReserved_Test(string word)
    {
        Assert.True(DslParser.IsReservedWord(word));
    }

    /// <summary>If takes exactly three arguments: condition, whenTrue, whenFalse.</summary>
    [Theory]
    [InlineData("If(a)")]
    [InlineData("If(a, b)")]
    [InlineData("If(a, b, c, d)")]
    [InlineData("If()")]
    public void Compile_WithWrongOperandCount_ReportsMalformedTree_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MalformedTree);
    }

    /// <summary>JSON uses the 'if' op with [condition, whenTrue, whenFalse], round-trips, and reads the op name in any letter case.</summary>
    [Fact]
    public void PrintJson_If_RoundTripsWithTheOpName_Test()
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile("a ? b : NOT c").CompiledRule!;

        string json = original.PrintJson();
        CompiledRule<RuleTestContext> reparsed = Compiler
            .CompileJson(json.Replace("\"if\"", "\"IF\"", StringComparison.Ordinal))
            .CompiledRule!;

        Assert.Contains("\"op\":\"if\"", json.Replace(" ", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>YAML uses the same node shape and round-trips.</summary>
    [Fact]
    public void PrintYaml_If_RoundTripsWithTheOpName_Test()
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile("a ? b : NOT c").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<RuleTestContext> reparsed = Compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Contains("op: if", yaml, StringComparison.Ordinal);
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>The builder produces the same rule as the DSL.</summary>
    [Fact]
    public void If_Builder_CompilesToTheSameCanonicalTextAsDsl_Test()
    {
        RuleBuilder builder = RuleBuilder.If(
            RuleBuilder.Predicate("a"),
            RuleBuilder.Predicate("b"),
            RuleBuilder.Not(RuleBuilder.Predicate("c"))
        );

        CompilationResult<RuleTestContext> result = builder.Compile(Compiler);

        Assert.True(result.Succeeded);
        Assert.Equal("If(a, b, NOT c)", result.CompiledRule!.CanonicalText);
    }

    /// <summary>The operator description explains that an Unknown condition does not pick a branch.</summary>
    [Fact]
    public void Describe_If_ExplainsTheUnknownConditionRule_Test()
    {
        RuleDescription description = Compiler.Compile("If(a, b, c)").CompiledRule!.Describe();

        Assert.Equal("If", description.Label);
        Assert.Contains("Unknown", description.Description, StringComparison.Ordinal);
        Assert.Equal(["a", "b", "c"], description.Operands.Select(o => o.Label));
    }

    /// <summary>If has no symbolic or C-style spelling, so every tree style keeps the word.</summary>
    [Theory]
    [InlineData(OperatorStyle.Word)]
    [InlineData(OperatorStyle.Symbolic)]
    [InlineData(OperatorStyle.CStyle)]
    public void Print_If_KeepsTheWordInEveryStyle_Test(OperatorStyle style)
    {
        RuleDescription tree = Compiler.Compile("If(a, b, c)").CompiledRule!.Describe();

        Assert.Contains("If", PlainTextTreePrinter.Print(tree, style), StringComparison.Ordinal);
        Assert.Contains("If", MermaidTreePrinter.Print(tree, style), StringComparison.Ordinal);
    }
}
