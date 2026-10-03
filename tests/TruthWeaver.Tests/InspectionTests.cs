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
/// <c>IsTrue</c>, <c>IsFalse</c>, <c>IsUnknown</c> and <c>IsKnown</c> inspect a result's K3 state (ADR-0005 decision 3):
/// they always yield a definite <c>True</c> or <c>False</c>, so they never collapse or poison the enclosing rule.
/// </summary>
public sealed class InspectionTests
{
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .Build()
    );

    /// <summary>Every operator, in any letter case, matches the oracle for each of the three input values.</summary>
    [Theory]
    [InlineData("IsTrue")]
    [InlineData("istrue")]
    [InlineData("IsFalse")]
    [InlineData("ISFALSE")]
    [InlineData("IsUnknown")]
    [InlineData("isUnknown")]
    [InlineData("IsKnown")]
    [InlineData("ISKNOWN")]
    public async Task Evaluate_OverAllInputs_MatchesOracle_Test(string name)
    {
        K3Rule rule = K3Rule.TryCreate($"{name}(a)", 1)!;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(1))
        {
            Decision actual = await rule.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(OracleFor(name)(assignment[0]), actual.Result);
        }
    }

    /// <summary>The inspected operand may itself be any expression, including another inspection.</summary>
    [Theory]
    [InlineData("IsUnknown(a AND b)")]
    [InlineData("IsKnown(a OR NOT b)")]
    [InlineData("IsTrue(a XOR b)")]
    [InlineData("IsFalse(a ?? b)")]
    [InlineData("IsKnown(IsUnknown(a))")]
    [InlineData("IsTrue(a ? b : c)")]
    public async Task Evaluate_OverComposedOperands_NeverYieldsUnknown_Test(string text)
    {
        K3Rule rule = K3Rule.TryCreate(text, 3)!;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(3))
        {
            Decision actual = await rule.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.NotEqual(TruthValue.Unknown, actual.Result);
        }
    }

    /// <summary>An Unknown operand of IsUnknown is reported as True, which then drives the enclosing rule normally.</summary>
    [Theory]
    [InlineData(TruthValue.Unknown, TruthValue.True, TruthValue.True)]
    [InlineData(TruthValue.Unknown, TruthValue.False, TruthValue.False)]
    [InlineData(TruthValue.True, TruthValue.True, TruthValue.False)]
    [InlineData(TruthValue.False, TruthValue.True, TruthValue.False)]
    public async Task Evaluate_InsideALargerRule_DoesNotCollapseTheRest_Test(
        TruthValue first,
        TruthValue second,
        TruthValue expected
    )
    {
        K3Rule rule = K3Rule.TryCreate("IsUnknown(a) AND b", 2)!;

        Decision decision = await rule.EvaluateAsync([first, second], TestContext.Current.CancellationToken);

        Assert.Equal(expected, decision.Result);
    }

    /// <summary>Inspections feed ordinary K3 operators, so an Unknown elsewhere is still Unknown.</summary>
    [Fact]
    public async Task Evaluate_AlongsideAnUnknownSibling_LeavesTheSiblingUnknown_Test()
    {
        K3Rule rule = K3Rule.TryCreate("IsKnown(a) AND b", 2)!;

        Decision decision = await rule.EvaluateAsync(
            [TruthValue.True, TruthValue.Unknown],
            TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
    }

    /// <summary>
    /// A faulting predicate is Unknown, which the inspection reports as a plain True/False; the inspection itself adds
    /// no fault and the enclosing rule still evaluates its other operands.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_InspectingAFaultingPredicate_YieldsADefiniteValueAndKeepsGoing_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddThrowing("boom").AddConstant("ok", true).Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("IsUnknown(boom) AND ok").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
        Assert.Single(decision.Faults);
        Assert.All(decision.EvaluatedTree!.Children, c => Assert.False(c.NotEvaluated));
    }

    /// <summary>The evaluated tree labels the node with the operator and has the inspected operand as its only child.</summary>
    [Fact]
    public async Task EvaluateAsync_Inspection_ProducesALabelledNodeWithOneChild_Test()
    {
        K3Rule rule = K3Rule.TryCreate("IsUnknown(a)", 1)!;

        Decision decision = await rule.EvaluateAsync([TruthValue.Unknown], TestContext.Current.CancellationToken);

        Assert.Equal("IsUnknown", decision.EvaluatedTree!.NodeDescription);
        Assert.Equal(TruthValue.True, decision.EvaluatedTree.Result);
        EvaluatedNode child = Assert.Single(decision.EvaluatedTree.Children);
        Assert.Equal(TruthValue.Unknown, child.Result);
    }

    /// <summary>Every spelling prints as the canonical camel-case function call.</summary>
    [Theory]
    [InlineData("istrue(a)", "IsTrue(a)")]
    [InlineData("ISFALSE( a )", "IsFalse(a)")]
    [InlineData("IsUnknown(a AND b)", "IsUnknown(a AND b)")]
    [InlineData("isknown(NOT a)", "IsKnown(NOT a)")]
    [InlineData("NOT IsTrue(a)", "NOT IsTrue(a)")]
    [InlineData("IsTrue(a) AND IsKnown(b)", "IsTrue(a) AND IsKnown(b)")]
    [InlineData("IsTrue(a) XOR IsFalse(b)", "(IsTrue(a) XOR IsFalse(b))")]
    [InlineData("IsKnown(IsUnknown(a))", "IsKnown(IsUnknown(a))")]
    public void Compile_AnySpelling_ProducesTheCanonicalText_Test(string text, string expected)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.True(result.Succeeded);
        Assert.Equal(expected, result.CompiledRule!.CanonicalText);
    }

    /// <summary>The inspections are reserved so a predicate cannot shadow them.</summary>
    [Theory]
    [InlineData("istrue")]
    [InlineData("IsFalse")]
    [InlineData("ISUNKNOWN")]
    [InlineData("IsKnown")]
    public void IsReservedWord_Inspection_IsReserved_Test(string word)
    {
        Assert.True(DslParser.IsReservedWord(word));
    }

    /// <summary>An inspection takes exactly one operand.</summary>
    [Theory]
    [InlineData("IsTrue()")]
    [InlineData("IsFalse(a, b)")]
    [InlineData("IsUnknown(a, b, c)")]
    [InlineData("IsKnown()")]
    public void Compile_WithWrongOperandCount_ReportsMalformedTree_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MalformedTree);
    }

    /// <summary>A bare keyword without a call is not an expression.</summary>
    [Fact]
    public void Compile_InspectionWithoutParentheses_ReportsSyntaxError_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile("IsTrue a");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.SyntaxError);
    }

    /// <summary>JSON uses the camel-case op names, round-trips, and reads them in any letter case.</summary>
    [Theory]
    [InlineData("isTrue")]
    [InlineData("isFalse")]
    [InlineData("isUnknown")]
    [InlineData("isKnown")]
    public void PrintJson_Inspection_RoundTripsWithTheOpName_Test(string op)
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile($"{op}(a AND NOT b)").CompiledRule!;

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
    [InlineData("isTrue")]
    [InlineData("isFalse")]
    [InlineData("isUnknown")]
    [InlineData("isKnown")]
    public void PrintYaml_Inspection_RoundTripsWithTheOpName_Test(string op)
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile($"{op}(a AND NOT b)").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<RuleTestContext> reparsed = Compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Contains($"op: {op}", yaml, StringComparison.Ordinal);
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>A JSON inspection node with the wrong operand count is rejected by the compiler.</summary>
    [Fact]
    public void CompileJson_InspectionWithTwoOperands_ReportsMalformedTree_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.CompileJson(
            """{"op": "isTrue", "operands": [{"predicate": "a"}, {"predicate": "b"}]}"""
        );

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MalformedTree);
    }

    /// <summary>The builder produces the same rule as the DSL for every inspection.</summary>
    [Fact]
    public void Inspection_Builder_CompilesToTheSameCanonicalTextAsDsl_Test()
    {
        RuleBuilder builder = RuleBuilder.And(
            RuleBuilder.IsTrue(RuleBuilder.Predicate("a")),
            RuleBuilder.IsFalse(RuleBuilder.Predicate("b")),
            RuleBuilder.IsUnknown(RuleBuilder.Not(RuleBuilder.Predicate("c"))),
            RuleBuilder.IsKnown(RuleBuilder.Predicate("a"))
        );

        CompilationResult<RuleTestContext> result = builder.Compile(Compiler);

        Assert.True(result.Succeeded);
        Assert.Equal("IsTrue(a) AND IsFalse(b) AND IsUnknown(NOT c) AND IsKnown(a)", result.CompiledRule!.CanonicalText);
    }

    /// <summary>Each operator's description says what state it tests and that it never yields Unknown.</summary>
    [Theory]
    [InlineData("IsTrue(a)", "IsTrue")]
    [InlineData("IsFalse(a)", "IsFalse")]
    [InlineData("IsUnknown(a)", "IsUnknown")]
    [InlineData("IsKnown(a)", "IsKnown")]
    public void Describe_Inspection_ExplainsItsDefiniteResult_Test(string text, string label)
    {
        RuleDescription description = Compiler.Compile(text).CompiledRule!.Describe();

        Assert.Equal(label, description.Label);
        Assert.Contains("never Unknown", description.Description, StringComparison.Ordinal);
        Assert.Equal(["a"], description.Operands.Select(o => o.Label));
    }

    /// <summary>The inspections have no symbolic or C-style spelling, so every tree style keeps the word.</summary>
    [Theory]
    [InlineData(OperatorStyle.Word)]
    [InlineData(OperatorStyle.Symbolic)]
    [InlineData(OperatorStyle.CStyle)]
    public void Print_Inspection_KeepsTheWordInEveryStyle_Test(OperatorStyle style)
    {
        RuleDescription tree = Compiler.Compile("IsUnknown(a)").CompiledRule!.Describe();

        Assert.Contains("IsUnknown", PlainTextTreePrinter.Print(tree, style), StringComparison.Ordinal);
        Assert.Contains("IsUnknown", MermaidTreePrinter.Print(tree, style), StringComparison.Ordinal);
    }

    /// <summary>
    /// The analyzer knows an inspection is always definite: <c>IsUnknown(a) OR IsKnown(a)</c> covers every K3 state, so it
    /// is a genuine tautology, and <c>IsTrue(a) AND IsFalse(a)</c> a genuine contradiction.
    /// </summary>
    [Theory]
    [InlineData("IsUnknown(a) OR IsKnown(a)", DiagnosticCodes.StructuralTautology)]
    [InlineData("IsTrue(a) OR IsFalse(a) OR IsUnknown(a)", DiagnosticCodes.StructuralTautology)]
    [InlineData("IsTrue(a) AND IsFalse(a)", DiagnosticCodes.StructuralContradiction)]
    [InlineData("IsKnown(a) AND IsUnknown(a)", DiagnosticCodes.StructuralContradiction)]
    public void Compile_InspectionsCoveringOrExcludingEveryState_ReportsTheK3Verdict_Test(string text, string code)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.True(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == code);
    }

    private static Func<TruthValue, TruthValue> OracleFor(string name)
    {
        return name.ToUpperInvariant() switch
        {
            "ISTRUE" => K3Oracle.IsTrue,
            "ISFALSE" => K3Oracle.IsFalse,
            "ISUNKNOWN" => K3Oracle.IsUnknown,
            "ISKNOWN" => K3Oracle.IsKnown,
            _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };
    }
}
