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
/// <c>BETWEEN(min, max, ...)</c> is a first-class n-ary function-call node (ADR-0005 decisions 3a and 6) defined as
/// <c>AND(AtLeast(min, ...), AtMost(max, ...))</c> over the definitely-true / possibly-true cardinality interval.
/// </summary>
public sealed class BetweenTests
{
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .Build()
    );

    /// <summary>BETWEEN over 2..4 operands, every valid bound pair and every {T,F,U} assignment, matches the oracle.</summary>
    [Theory]
    [InlineData("BETWEEN")]
    [InlineData("between")]
    [InlineData("Between")]
    public async Task Evaluate_OverAllBoundsAndAssignments_MatchesOracle_Test(string keyword)
    {
        for (int arity = 2; arity <= 4; arity++)
        {
            string names = string.Join(", ", Enumerable.Range(0, arity).Select(i => (char)('a' + i)));
            for (int min = 0; min <= arity; min++)
            {
                for (int max = min; max <= arity; max++)
                {
                    if (min == 0 && max == arity)
                    {
                        continue; // The whole range is a structural constant and is rejected (see the bounds test).
                    }

                    K3Rule rule = K3Rule.TryCreate($"{keyword}({min}, {max}, {names})", arity)!;

                    foreach (TruthValue[] assignment in K3Oracle.Assignments(arity))
                    {
                        Decision actual = await rule.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

                        Assert.Equal(K3Oracle.Between(min, max, assignment), actual.Result);
                    }
                }
            }
        }
    }

    /// <summary>BETWEEN agrees with the AND of the two threshold rules it is defined as, for every assignment.</summary>
    [Fact]
    public async Task Evaluate_OverAllAssignments_AgreesWithItsThresholdDefinition_Test()
    {
        K3Rule between = K3Rule.TryCreate("BETWEEN(1, 2, a, b, c)", 3)!;
        K3Rule definition = K3Rule.TryCreate("AtLeast(1, a, b, c) AND AtMost(2, a, b, c)", 3)!;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(3))
        {
            Decision expected = await definition.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
            Decision actual = await between.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(expected.Result, actual.Result);
        }
    }

    /// <summary>Spot-check the interval rule: an out-of-range definite count is False even beside Unknown, an in-range one needs no Unknown.</summary>
    [Theory]
    [InlineData("BETWEEN(1, 1, a, b, c)", TruthValue.True, TruthValue.True, TruthValue.Unknown, TruthValue.False)]
    [InlineData("BETWEEN(1, 1, a, b, c)", TruthValue.True, TruthValue.False, TruthValue.False, TruthValue.True)]
    [InlineData("BETWEEN(1, 2, a, b, c)", TruthValue.True, TruthValue.Unknown, TruthValue.False, TruthValue.True)]
    [InlineData("BETWEEN(1, 2, a, b, c)", TruthValue.True, TruthValue.Unknown, TruthValue.Unknown, TruthValue.Unknown)]
    [InlineData("BETWEEN(1, 2, a, b, c)", TruthValue.False, TruthValue.False, TruthValue.False, TruthValue.False)]
    public async Task Evaluate_WithUnknown_FollowsTheCardinalityInterval_Test(
        string text,
        TruthValue first,
        TruthValue second,
        TruthValue third,
        TruthValue expected
    )
    {
        K3Rule rule = K3Rule.TryCreate(text, 3)!;

        Decision decision = await rule.EvaluateAsync([first, second, third], TestContext.Current.CancellationToken);

        Assert.Equal(expected, decision.Result);
    }

    /// <summary>Every spelling prints as the canonical upper-case function-call form with the bounds first.</summary>
    [Theory]
    [InlineData("BETWEEN(1, 2, a, b, c)", "BETWEEN(1, 2, a, b, c)")]
    [InlineData("between(1,2,a,b,c)", "BETWEEN(1, 2, a, b, c)")]
    [InlineData("Between( 0 , 1 , a , b )", "BETWEEN(0, 1, a, b)")]
    public void Compile_AnySpelling_ProducesTheCanonicalText_Test(string text, string expected)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.True(result.Succeeded);
        Assert.Equal(expected, result.CompiledRule!.CanonicalText);
    }

    /// <summary>A function-call form has no precedence, so it combines with infix operators without parentheses.</summary>
    [Theory]
    [InlineData("BETWEEN(1, 2, a, b, c) AND c")]
    [InlineData("a OR BETWEEN(1, 1, b, c)")]
    [InlineData("BETWEEN(1, 1, a, b) XOR c")]
    [InlineData("NOT BETWEEN(1, 2, a, b, c)")]
    [InlineData("BETWEEN(1, 2, a AND b, c XOR a, BETWEEN(1, 1, a, b))")]
    public void Compile_NextToOtherOperators_NeedsNoParentheses_Test(string text)
    {
        Assert.True(Compiler.Compile(text).Succeeded);
    }

    /// <summary>The operator name is reserved so a predicate cannot shadow it.</summary>
    [Theory]
    [InlineData("between")]
    [InlineData("BETWEEN")]
    public void IsReservedWord_Between_IsReserved_Test(string word)
    {
        Assert.True(DslParser.IsReservedWord(word));
    }

    /// <summary>Invalid bounds give a readable threshold-value diagnostic that names both bounds and the allowed range.</summary>
    [Theory]
    [InlineData("BETWEEN(2, 1, a, b, c)", "min=2")]
    [InlineData("BETWEEN(-1, 1, a, b, c)", "min=-1")]
    [InlineData("BETWEEN(1, 4, a, b, c)", "max=4")]
    [InlineData("BETWEEN(4, 5, a, b, c)", "min=4")]
    [InlineData("BETWEEN(0, 3, a, b, c)", "always True")]
    public void Compile_WithInvalidBounds_ReportsInvalidThresholdValue_Test(string text, string expectedFragment)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.InvalidThresholdValue);
        Assert.Contains(expectedFragment, diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("BETWEEN", diagnostic.Message, StringComparison.Ordinal);
    }

    /// <summary>A missing or non-integer bound is a syntax error that names the bound.</summary>
    [Theory]
    [InlineData("BETWEEN(a, b)", "minimum")]
    [InlineData("BETWEEN(1, a, b)", "maximum")]
    [InlineData("BETWEEN(1.5, 2, a, b)", "minimum")]
    public void Compile_WithoutIntegerBounds_ReportsSyntaxError_Test(string text, string bound)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Diagnostics,
            d => d.Code == DiagnosticCodes.SyntaxError && d.Message.Contains(bound, StringComparison.Ordinal)
        );
    }

    /// <summary>Like ANY, ALL and ExactlyOne, BETWEEN needs at least two operands.</summary>
    [Theory]
    [InlineData("BETWEEN(0, 0, a)")]
    [InlineData("BETWEEN(0, 0)")]
    public void Compile_WithFewerThanTwoOperands_ReportsMalformedTree_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MalformedTree);
    }

    /// <summary>JSON carries the bounds as 'min' and 'max', round-trips, and reads the op name in any letter case.</summary>
    [Fact]
    public void PrintJson_Between_RoundTripsWithTheBounds_Test()
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile("BETWEEN(1, 2, a, b, c)").CompiledRule!;

        string json = original.PrintJson();
        CompiledRule<RuleTestContext> reparsed = Compiler
            .CompileJson(json.Replace("between", "BETWEEN", StringComparison.Ordinal))
            .CompiledRule!;

        string compact = json.Replace(" ", string.Empty, StringComparison.Ordinal);
        Assert.Contains("\"op\":\"between\"", compact, StringComparison.Ordinal);
        Assert.Contains("\"min\":1", compact, StringComparison.Ordinal);
        Assert.Contains("\"max\":2", compact, StringComparison.Ordinal);
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>A JSON BETWEEN without a numeric 'min' or 'max' is a malformed tree.</summary>
    [Theory]
    [InlineData("""{"op":"between","max":1,"operands":[{"predicate":"a"},{"predicate":"b"}]}""")]
    [InlineData("""{"op":"between","min":0,"operands":[{"predicate":"a"},{"predicate":"b"}]}""")]
    [InlineData("""{"op":"between","min":"0","max":1,"operands":[{"predicate":"a"},{"predicate":"b"}]}""")]
    public void CompileJson_BetweenWithoutNumericBounds_ReportsMalformedTree_Test(string json)
    {
        CompilationResult<RuleTestContext> result = Compiler.CompileJson(json);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MalformedTree);
    }

    /// <summary>YAML uses the same node shape and round-trips.</summary>
    [Fact]
    public void PrintYaml_Between_RoundTripsWithTheBounds_Test()
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile("BETWEEN(1, 2, a, b, c)").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<RuleTestContext> reparsed = Compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Contains("op: between", yaml, StringComparison.Ordinal);
        Assert.Contains("min: 1", yaml, StringComparison.Ordinal);
        Assert.Contains("max: 2", yaml, StringComparison.Ordinal);
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>A YAML BETWEEN without a numeric 'min' or 'max' is a malformed tree.</summary>
    [Fact]
    public void CompileYaml_BetweenWithoutBounds_ReportsMalformedTree_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.CompileYaml(
            "op: between\nmin: 0\noperands:\n  - predicate: a\n  - predicate: b\n"
        );

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MalformedTree);
    }

    /// <summary>The builder produces the same rule as the DSL.</summary>
    [Fact]
    public void Between_Builder_CompilesToTheSameCanonicalTextAsDsl_Test()
    {
        RuleBuilder builder = RuleBuilder.Between(
            1,
            2,
            RuleBuilder.Predicate("a"),
            RuleBuilder.Not(RuleBuilder.Predicate("b")),
            RuleBuilder.Predicate("c")
        );

        CompilationResult<RuleTestContext> result = builder.Compile(Compiler);

        Assert.True(result.Succeeded);
        Assert.Equal("BETWEEN(1, 2, a, NOT b, c)", result.CompiledRule!.CanonicalText);
    }

    /// <summary>The evaluated tree labels the node with both bounds and keeps operand order.</summary>
    [Fact]
    public async Task EvaluateAsync_Between_LabelsTheNodeWithItsBoundsAndKeepsOperandOrder_Test()
    {
        K3Rule rule = K3Rule.TryCreate("BETWEEN(1, 2, a, b, c)", 3)!;

        Decision decision = await rule.EvaluateAsync(
            [TruthValue.True, TruthValue.False, TruthValue.True],
            TestContext.Current.CancellationToken
        );

        Assert.Equal("BETWEEN(1, 2)", decision.EvaluatedTree!.NodeDescription);
        Assert.Equal(["a", "b", "c"], decision.EvaluatedTree.Children.Select(c => c.NodeDescription));
    }

    /// <summary>The operator description states the interval.</summary>
    [Fact]
    public void Describe_Between_ExplainsTheInterval_Test()
    {
        RuleDescription description = Compiler.Compile("BETWEEN(1, 2, a, b, c)").CompiledRule!.Describe();

        Assert.Equal("BETWEEN(1, 2)", description.Label);
        Assert.Contains("between 1 and 2", description.Description, StringComparison.Ordinal);
        Assert.Equal(["a", "b", "c"], description.Operands.Select(o => o.Label));
    }

    /// <summary>Tree renderings keep the function-call word in every operator style (there is no symbol).</summary>
    [Theory]
    [InlineData(OperatorStyle.Word)]
    [InlineData(OperatorStyle.Symbolic)]
    [InlineData(OperatorStyle.CStyle)]
    public void Print_Between_KeepsTheWordInEveryStyle_Test(OperatorStyle style)
    {
        RuleDescription tree = Compiler.Compile("BETWEEN(1, 2, a, b, c)").CompiledRule!.Describe();

        Assert.Contains("BETWEEN(1, 2)", PlainTextTreePrinter.Print(tree, style), StringComparison.Ordinal);
        Assert.Contains("BETWEEN(1, 2)", MermaidTreePrinter.Print(tree, style), StringComparison.Ordinal);
    }
}
