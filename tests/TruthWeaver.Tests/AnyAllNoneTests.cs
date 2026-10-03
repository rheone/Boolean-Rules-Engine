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
/// <c>ANY</c>, <c>ALL</c> and <c>NONE</c> are first-class n-ary function-call nodes (ADR-0005 decisions 3a and 6)
/// with the cardinality-interval semantics of <c>AtLeast(1, ...)</c>, <c>AtLeast(n, ...)</c> and <c>AtMost(0, ...)</c>.
/// </summary>
public sealed class AnyAllNoneTests
{
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .Build()
    );

    /// <summary>ANY, ALL and NONE over 2..4 operands, in upper, lower and mixed case, match the oracle for every assignment.</summary>
    [Theory]
    [InlineData("ANY")]
    [InlineData("any")]
    [InlineData("Any")]
    [InlineData("ALL")]
    [InlineData("all")]
    [InlineData("All")]
    [InlineData("NONE")]
    [InlineData("none")]
    [InlineData("None")]
    public async Task Evaluate_OverAllAssignments_MatchesOracle_Test(string keyword)
    {
        Func<IReadOnlyList<TruthValue>, TruthValue> oracle = OracleFor(keyword);
        for (int arity = 2; arity <= 4; arity++)
        {
            string names = string.Join(", ", Enumerable.Range(0, arity).Select(i => (char)('a' + i)));
            K3Rule rule = K3Rule.TryCreate($"{keyword}({names})", arity)!;

            foreach (TruthValue[] assignment in K3Oracle.Assignments(arity))
            {
                Decision actual = await rule.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

                Assert.Equal(oracle(assignment), actual.Result);
            }
        }
    }

    /// <summary>Each operator agrees with the threshold form it is defined as, for every assignment.</summary>
    [Theory]
    [InlineData("ANY", "AtLeast(1, {0})")]
    [InlineData("ALL", "AtLeast({1}, {0})")]
    [InlineData("NONE", "AtMost(0, {0})")]
    public async Task Evaluate_OverAllAssignments_AgreesWithItsThresholdDefinition_Test(
        string keyword,
        string thresholdTemplate
    )
    {
        for (int arity = 2; arity <= 4; arity++)
        {
            string names = string.Join(", ", Enumerable.Range(0, arity).Select(i => (char)('a' + i)));
            K3Rule derived = K3Rule.TryCreate($"{keyword}({names})", arity)!;
            K3Rule threshold = K3Rule.TryCreate(string.Format(thresholdTemplate, names, arity), arity)!;

            foreach (TruthValue[] assignment in K3Oracle.Assignments(arity))
            {
                Decision expected = await threshold.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
                Decision actual = await derived.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

                Assert.Equal(expected.Result, actual.Result);
            }
        }
    }

    /// <summary>Spot-check the interval rule: a True settles ANY and a False settles ALL even beside Unknown, while NONE needs every operand False.</summary>
    [Theory]
    [InlineData("ANY(a, b)", TruthValue.True, TruthValue.Unknown, TruthValue.True)]
    [InlineData("ANY(a, b)", TruthValue.False, TruthValue.Unknown, TruthValue.Unknown)]
    [InlineData("ALL(a, b)", TruthValue.False, TruthValue.Unknown, TruthValue.False)]
    [InlineData("ALL(a, b)", TruthValue.True, TruthValue.Unknown, TruthValue.Unknown)]
    [InlineData("NONE(a, b)", TruthValue.True, TruthValue.Unknown, TruthValue.False)]
    [InlineData("NONE(a, b)", TruthValue.False, TruthValue.Unknown, TruthValue.Unknown)]
    [InlineData("NONE(a, b)", TruthValue.False, TruthValue.False, TruthValue.True)]
    public async Task Evaluate_WithUnknown_FollowsTheCardinalityInterval_Test(
        string text,
        TruthValue first,
        TruthValue second,
        TruthValue expected
    )
    {
        K3Rule rule = K3Rule.TryCreate(text, 2)!;

        Decision decision = await rule.EvaluateAsync([first, second], TestContext.Current.CancellationToken);

        Assert.Equal(expected, decision.Result);
    }

    /// <summary>Every spelling prints as the canonical upper-case function-call form.</summary>
    [Theory]
    [InlineData("ANY(a, b, c)", "ANY(a, b, c)")]
    [InlineData("any(a,b,c)", "ANY(a, b, c)")]
    [InlineData("All( a , b )", "ALL(a, b)")]
    [InlineData("all(a, b)", "ALL(a, b)")]
    [InlineData("None(a, b, c)", "NONE(a, b, c)")]
    [InlineData("none(a,b)", "NONE(a, b)")]
    public void Compile_AnySpelling_ProducesTheCanonicalText_Test(string text, string expected)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.True(result.Succeeded);
        Assert.Equal(expected, result.CompiledRule!.CanonicalText);
    }

    /// <summary>A function-call form has no precedence, so it combines with infix operators without parentheses.</summary>
    [Theory]
    [InlineData("ANY(a, b) AND c")]
    [InlineData("a OR ALL(b, c)")]
    [InlineData("NONE(a, b) XOR c")]
    [InlineData("NOT ANY(a, b, c)")]
    [InlineData("ALL(a AND b, c XOR a, NONE(a, b))")]
    public void Compile_NextToOtherOperators_NeedsNoParentheses_Test(string text)
    {
        Assert.True(Compiler.Compile(text).Succeeded);
    }

    /// <summary>The operator names are reserved so a predicate cannot shadow them.</summary>
    [Theory]
    [InlineData("any")]
    [InlineData("ALL")]
    [InlineData("None")]
    public void IsReservedWord_AnyAllNone_IsReserved_Test(string word)
    {
        Assert.True(DslParser.IsReservedWord(word));
    }

    /// <summary>Like AND, OR and ExactlyOne, the operators need at least two operands.</summary>
    [Theory]
    [InlineData("ANY(a)")]
    [InlineData("ALL()")]
    [InlineData("NONE(a)")]
    public void Compile_WithFewerThanTwoOperands_ReportsMalformedTree_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MalformedTree);
    }

    /// <summary>JSON uses lower-case op names, round-trips, and reads the op name in any letter case.</summary>
    [Theory]
    [InlineData("ANY(a, b, c)", "any")]
    [InlineData("ALL(a, b, c)", "all")]
    [InlineData("NONE(a, b, c)", "none")]
    public void PrintJson_AnyAllNone_RoundTripsWithTheOpName_Test(string text, string op)
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile(text).CompiledRule!;

        string json = original.PrintJson();
        CompiledRule<RuleTestContext> reparsed = Compiler
            .CompileJson(json.Replace(op, op.ToUpperInvariant(), StringComparison.Ordinal))
            .CompiledRule!;

        Assert.Contains(
            $"\"op\":\"{op}\"",
            json.Replace(" ", string.Empty, StringComparison.Ordinal),
            StringComparison.Ordinal
        );
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>YAML uses the same node shape and round-trips.</summary>
    [Theory]
    [InlineData("ANY(a, b, c)", "any")]
    [InlineData("ALL(a, b, c)", "all")]
    [InlineData("NONE(a, b, c)", "none")]
    public void PrintYaml_AnyAllNone_RoundTripsWithTheOpName_Test(string text, string op)
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile(text).CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<RuleTestContext> reparsed = Compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Contains($"op: {op}", yaml, StringComparison.Ordinal);
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>The builder produces the same rules as the DSL.</summary>
    [Fact]
    public void AnyAllNone_Builders_CompileToTheSameCanonicalTextAsDsl_Test()
    {
        RuleBuilder[] operands =
        [
            RuleBuilder.Predicate("a"),
            RuleBuilder.Not(RuleBuilder.Predicate("b")),
            RuleBuilder.Predicate("c"),
        ];

        Assert.Equal("ANY(a, NOT b, c)", RuleBuilder.Any(operands).Compile(Compiler).CompiledRule!.CanonicalText);
        Assert.Equal("ALL(a, NOT b, c)", RuleBuilder.All(operands).Compile(Compiler).CompiledRule!.CanonicalText);
        Assert.Equal("NONE(a, NOT b, c)", RuleBuilder.None(operands).Compile(Compiler).CompiledRule!.CanonicalText);
    }

    /// <summary>The evaluated tree labels the node with the operator name and keeps operand order.</summary>
    [Theory]
    [InlineData("ANY(a, b, c)", "ANY")]
    [InlineData("ALL(a, b, c)", "ALL")]
    [InlineData("NONE(a, b, c)", "NONE")]
    public async Task EvaluateAsync_AnyAllNone_LabelsTheNodeAndKeepsOperandOrder_Test(string text, string label)
    {
        K3Rule rule = K3Rule.TryCreate(text, 3)!;

        Decision decision = await rule.EvaluateAsync(
            [TruthValue.True, TruthValue.False, TruthValue.True],
            TestContext.Current.CancellationToken
        );

        Assert.Equal(label, decision.EvaluatedTree!.NodeDescription);
        Assert.Equal(["a", "b", "c"], decision.EvaluatedTree.Children.Select(c => c.NodeDescription));
    }

    /// <summary>The operator descriptions state the cardinality each stands for.</summary>
    [Theory]
    [InlineData("ANY(a, b)", "ANY", "at least one")]
    [InlineData("ALL(a, b)", "ALL", "every operand")]
    [InlineData("NONE(a, b)", "NONE", "no operand")]
    public void Describe_AnyAllNone_ExplainsTheCardinality_Test(string text, string label, string phrase)
    {
        RuleDescription description = Compiler.Compile(text).CompiledRule!.Describe();

        Assert.Equal(label, description.Label);
        Assert.Contains(phrase, description.Description, StringComparison.Ordinal);
        Assert.Equal(["a", "b"], description.Operands.Select(o => o.Label));
    }

    /// <summary>Tree renderings keep the function-call word in every operator style (there is no symbol).</summary>
    [Theory]
    [InlineData("ANY(a, b)", "ANY", OperatorStyle.Word)]
    [InlineData("ANY(a, b)", "ANY", OperatorStyle.Symbolic)]
    [InlineData("ALL(a, b)", "ALL", OperatorStyle.CStyle)]
    [InlineData("ALL(a, b)", "ALL", OperatorStyle.Symbolic)]
    [InlineData("NONE(a, b)", "NONE", OperatorStyle.Word)]
    [InlineData("NONE(a, b)", "NONE", OperatorStyle.CStyle)]
    public void Print_AnyAllNone_KeepsTheWordInEveryStyle_Test(string text, string label, OperatorStyle style)
    {
        RuleDescription tree = Compiler.Compile(text).CompiledRule!.Describe();

        Assert.Contains(label, PlainTextTreePrinter.Print(tree, style), StringComparison.Ordinal);
        Assert.Contains(label, MermaidTreePrinter.Print(tree, style), StringComparison.Ordinal);
    }

    private static Func<IReadOnlyList<TruthValue>, TruthValue> OracleFor(string keyword)
    {
        return keyword.ToUpperInvariant() switch
        {
            "ANY" => K3Oracle.Any,
            "ALL" => K3Oracle.All,
            _ => K3Oracle.None,
        };
    }
}
