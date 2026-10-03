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
/// <c>Collapse(expr, policy)</c> is the evaluation-API boundary (ADR-0005 decision 14): it turns a K3 result into a
/// two-valued answer, <c>UnknownIsError</c> yields an explicit rejected outcome that is neither a fault nor an exception,
/// <c>Decision.IsSatisfied</c> stays fail-closed, and in the DSL the function is accepted only as the outermost expression.
/// </summary>
public sealed class CollapseTests
{
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .Build()
    );

    /// <summary>Every policy spelling, in any letter case, applied to every input value matches the oracle through the whole pipeline.</summary>
    [Theory]
    [InlineData("Collapse(a, UnknownAsFalse)", CollapsePolicy.UnknownAsFalse)]
    [InlineData("collapse(a, unknownasfalse)", CollapsePolicy.UnknownAsFalse)]
    [InlineData("Collapse(a, UnknownAsTrue)", CollapsePolicy.UnknownAsTrue)]
    [InlineData("COLLAPSE(a, UNKNOWNASTRUE)", CollapsePolicy.UnknownAsTrue)]
    [InlineData("Collapse(a, UnknownIsError)", CollapsePolicy.UnknownIsError)]
    [InlineData("collapse(a, UnknownIsError)", CollapsePolicy.UnknownIsError)]
    public async Task EvaluateAsync_DeclaredCollapse_OverAllInputs_MatchesOracle_Test(string text, CollapsePolicy policy)
    {
        K3Rule rule = K3Rule.TryCreate(text, 1)!;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(1))
        {
            Decision actual = await rule.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(K3Oracle.Collapse(assignment[0], policy), actual.Outcome);
            Assert.Empty(actual.Faults);
        }
    }

    /// <summary>A declared collapse wraps a whole expression, whose K3 result is the one collapsed.</summary>
    [Theory]
    [InlineData(CollapsePolicy.UnknownAsFalse)]
    [InlineData(CollapsePolicy.UnknownAsTrue)]
    [InlineData(CollapsePolicy.UnknownIsError)]
    public async Task EvaluateAsync_DeclaredCollapse_OverAComposedExpression_CollapsesItsK3Result_Test(CollapsePolicy policy)
    {
        K3Rule rule = K3Rule.TryCreate($"Collapse(a AND (b OR NOT c), {policy})", 3)!;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(3))
        {
            TruthValue inner = K3Oracle.And([assignment[0], K3Oracle.Or([assignment[1], K3Oracle.Not(assignment[2])])]);

            Decision actual = await rule.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(K3Oracle.Collapse(inner, policy), actual.Outcome);
        }
    }

    /// <summary>
    /// With <c>UnknownAsFalse</c>/<c>UnknownAsTrue</c> the declared collapse makes the decision's result definite, so the
    /// result and the outcome agree; with <c>UnknownIsError</c> the result stays the K3 value and the outcome says rejected.
    /// </summary>
    [Theory]
    [InlineData("Collapse(a, UnknownAsFalse)", TruthValue.Unknown, TruthValue.False, CollapseOutcome.False)]
    [InlineData("Collapse(a, UnknownAsTrue)", TruthValue.Unknown, TruthValue.True, CollapseOutcome.True)]
    [InlineData("Collapse(a, UnknownIsError)", TruthValue.Unknown, TruthValue.Unknown, CollapseOutcome.RejectedUnresolved)]
    [InlineData("Collapse(a, UnknownIsError)", TruthValue.True, TruthValue.True, CollapseOutcome.True)]
    [InlineData("Collapse(a, UnknownAsTrue)", TruthValue.False, TruthValue.False, CollapseOutcome.False)]
    public async Task EvaluateAsync_DeclaredCollapse_SetsTheResultAndTheOutcome_Test(
        string text,
        TruthValue input,
        TruthValue expectedResult,
        CollapseOutcome expectedOutcome
    )
    {
        K3Rule rule = K3Rule.TryCreate(text, 1)!;

        Decision decision = await rule.EvaluateAsync([input], TestContext.Current.CancellationToken);

        Assert.Equal(expectedResult, decision.Result);
        Assert.Equal(expectedOutcome, decision.Outcome);
    }

    /// <summary>A rule without a declared collapse reports no outcome and keeps its raw K3 result.</summary>
    [Fact]
    public async Task EvaluateAsync_WithoutACollapse_HasNoOutcomeAndTheRawResult_Test()
    {
        K3Rule rule = K3Rule.TryCreate("a", 1)!;

        Decision decision = await rule.EvaluateAsync([TruthValue.Unknown], TestContext.Current.CancellationToken);

        Assert.Null(decision.Outcome);
        Assert.Equal(TruthValue.Unknown, decision.Result);
    }

    /// <summary>
    /// The API boundary form: <c>Decision.Collapse(policy)</c> over every K3 result and every policy matches the oracle,
    /// without any declared collapse in the rule.
    /// </summary>
    [Theory]
    [InlineData(CollapsePolicy.UnknownAsFalse)]
    [InlineData(CollapsePolicy.UnknownAsTrue)]
    [InlineData(CollapsePolicy.UnknownIsError)]
    public async Task Decision_Collapse_OverAllInputs_MatchesOracle_Test(CollapsePolicy policy)
    {
        K3Rule rule = K3Rule.TryCreate("a", 1)!;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(1))
        {
            Decision decision = await rule.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(K3Oracle.Collapse(assignment[0], policy), decision.Collapse(policy));
        }
    }

    /// <summary><c>UnknownIsError</c> on a merely unknown result is a rejected outcome with no fault and no exception.</summary>
    [Fact]
    public async Task EvaluateAsync_UnknownIsErrorOverAnUnknown_IsRejectedWithoutAFault_Test()
    {
        K3Rule rule = K3Rule.TryCreate("Collapse(a AND b, UnknownIsError)", 2)!;

        Decision decision = await rule.EvaluateAsync(
            [TruthValue.True, TruthValue.Unknown],
            TestContext.Current.CancellationToken
        );

        Assert.Equal(CollapseOutcome.RejectedUnresolved, decision.Outcome);
        Assert.Empty(decision.Faults);
    }

    /// <summary>
    /// "Not known" and "something broke" stay distinguishable: a faulting predicate yields the same rejected outcome but the
    /// fault is on <c>Decision.Faults</c>, which the clean unknown above does not have.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_UnknownIsErrorOverAFaultingPredicate_IsRejectedAndKeepsTheFault_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddThrowing("boom").Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("Collapse(boom, UnknownIsError)").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(CollapseOutcome.RejectedUnresolved, decision.Outcome);
        Assert.Single(decision.Faults);
    }

    /// <summary>A faulting predicate under a lenient policy still records its fault; collapsing never hides one.</summary>
    [Theory]
    [InlineData("Collapse(boom, UnknownAsFalse)", CollapseOutcome.False)]
    [InlineData("Collapse(boom, UnknownAsTrue)", CollapseOutcome.True)]
    public async Task EvaluateAsync_LenientCollapseOverAFaultingPredicate_KeepsTheFault_Test(
        string text,
        CollapseOutcome expected
    )
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddThrowing("boom").Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile(text).CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(expected, decision.Outcome);
        Assert.Single(decision.Faults);
    }

    /// <summary>
    /// <c>Decision.IsSatisfied</c> stays fail-closed: true only for a <c>True</c> result. Calling
    /// <c>Collapse(UnknownAsTrue)</c> on an <c>Unknown</c> decision does not change that.
    /// </summary>
    [Theory]
    [InlineData(TruthValue.True, true)]
    [InlineData(TruthValue.False, false)]
    [InlineData(TruthValue.Unknown, false)]
    public async Task IsSatisfied_WithoutADeclaredCollapse_IsTrueOnlyForTrue_Test(TruthValue input, bool expected)
    {
        K3Rule rule = K3Rule.TryCreate("a", 1)!;

        Decision decision = await rule.EvaluateAsync([input], TestContext.Current.CancellationToken);
        CollapseOutcome lenient = decision.Collapse(CollapsePolicy.UnknownAsTrue);

        Assert.Equal(expected, decision.IsSatisfied);
        Assert.Equal(input == TruthValue.False ? CollapseOutcome.False : CollapseOutcome.True, lenient);
        Assert.Equal(expected, decision.IsSatisfied);
    }

    /// <summary>
    /// Under a declared <c>UnknownIsError</c> collapse a rejected decision is not satisfied, and a known one follows its
    /// value: <c>IsSatisfied</c> is true only for <c>True</c>.
    /// </summary>
    [Theory]
    [InlineData(TruthValue.True, true)]
    [InlineData(TruthValue.False, false)]
    [InlineData(TruthValue.Unknown, false)]
    public async Task IsSatisfied_UnderUnknownIsError_IsTrueOnlyForTrue_Test(TruthValue input, bool expected)
    {
        K3Rule rule = K3Rule.TryCreate("Collapse(a, UnknownIsError)", 1)!;

        Decision decision = await rule.EvaluateAsync([input], TestContext.Current.CancellationToken);

        Assert.Equal(expected, decision.IsSatisfied);
    }

    /// <summary>
    /// A declared lenient collapse is the caller's explicit choice, so its definite result is what <c>IsSatisfied</c> reads:
    /// <c>UnknownAsTrue</c> over <c>Unknown</c> is a <c>True</c> result and therefore satisfied.
    /// </summary>
    [Theory]
    [InlineData("Collapse(a, UnknownAsTrue)", TruthValue.Unknown, true)]
    [InlineData("Collapse(a, UnknownAsFalse)", TruthValue.Unknown, false)]
    [InlineData("Collapse(a, UnknownAsFalse)", TruthValue.True, true)]
    public async Task IsSatisfied_UnderADeclaredLenientCollapse_FollowsTheCollapsedResult_Test(
        string text,
        TruthValue input,
        bool expected
    )
    {
        K3Rule rule = K3Rule.TryCreate(text, 1)!;

        Decision decision = await rule.EvaluateAsync([input], TestContext.Current.CancellationToken);

        Assert.Equal(expected, decision.IsSatisfied);
    }

    /// <summary>The compiled rule remembers its declared policy; a plain rule has none.</summary>
    [Theory]
    [InlineData("Collapse(a, UnknownAsTrue)", CollapsePolicy.UnknownAsTrue)]
    [InlineData("collapse(a AND b, unknownIsError)", CollapsePolicy.UnknownIsError)]
    public void Compile_DeclaredCollapse_IsRememberedOnTheCompiledRule_Test(string text, CollapsePolicy expected)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.True(result.Succeeded);
        Assert.Equal(expected, result.CompiledRule!.CollapsePolicy);
    }

    /// <summary>A rule with no collapse declares no policy.</summary>
    [Fact]
    public void Compile_WithoutACollapse_HasNoPolicy_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile("a AND b");

        Assert.Null(result.CompiledRule!.CollapsePolicy);
    }

    /// <summary>The canonical text writes the outermost function with the policy in canonical case and recompiles to the same rule.</summary>
    [Theory]
    [InlineData("collapse(a, unknownasfalse)", "Collapse(a, UnknownAsFalse)")]
    [InlineData("COLLAPSE( a AND b , UNKNOWNIsError )", "Collapse(a AND b, UnknownIsError)")]
    [InlineData("Collapse(a XOR b, UnknownAsTrue)", "Collapse((a XOR b), UnknownAsTrue)")]
    [InlineData("Collapse(Project(a, True), UnknownIsError)", "Collapse(Project(a, True), UnknownIsError)")]
    public void Compile_AnySpelling_ProducesTheCanonicalTextAndRoundTrips_Test(string text, string expected)
    {
        CompiledRule<RuleTestContext> rule = Compiler.Compile(text).CompiledRule!;
        CompiledRule<RuleTestContext> reparsed = Compiler.Compile(rule.CanonicalText).CompiledRule!;

        Assert.Equal(expected, rule.CanonicalText);
        Assert.Equal(rule.CanonicalText, reparsed.CanonicalText);
        Assert.Equal(rule.CollapsePolicy, reparsed.CollapsePolicy);
    }

    /// <summary><c>Collapse</c> is reserved so a predicate cannot shadow it.</summary>
    [Theory]
    [InlineData("collapse")]
    [InlineData("Collapse")]
    [InlineData("COLLAPSE")]
    public void IsReservedWord_Collapse_IsReserved_Test(string word)
    {
        Assert.True(DslParser.IsReservedWord(word));
    }

    /// <summary>
    /// A collapse anywhere but the outermost position is a diagnostic whose span covers exactly the nested collapse
    /// expression.
    /// </summary>
    [Theory]
    [InlineData("a AND Collapse(b, UnknownAsFalse)", "Collapse(b, UnknownAsFalse)")]
    [InlineData("Collapse(a, UnknownAsFalse) OR b", "Collapse(a, UnknownAsFalse)")]
    [InlineData("NOT Collapse(a, UnknownIsError)", "Collapse(a, UnknownIsError)")]
    [InlineData("Collapse(Collapse(a, UnknownAsFalse), UnknownAsTrue)", "Collapse(a, UnknownAsFalse)")]
    [InlineData("Collapse(a AND Collapse(b, UnknownAsTrue), UnknownAsFalse)", "Collapse(b, UnknownAsTrue)")]
    [InlineData("If(a, Collapse(b, UnknownAsFalse), c)", "Collapse(b, UnknownAsFalse)")]
    [InlineData("Project(Collapse(a, UnknownAsFalse), True)", "Collapse(a, UnknownAsFalse)")]
    [InlineData("ANY(a, Collapse(b, UnknownAsFalse))", "Collapse(b, UnknownAsFalse)")]
    public void Compile_NestedCollapse_ReportsADiagnosticAtTheNestedCollapse_Test(string text, string nested)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Diagnostic error = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.NestedCollapse);
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Contains("outermost", error.Message, StringComparison.Ordinal);
        Assert.Equal(nested, text.Substring(error.Span.Start, error.Span.Length));
    }

    /// <summary>Two nested collapses are two diagnostics, one per occurrence.</summary>
    [Fact]
    public void Compile_TwoNestedCollapses_ReportsEachOne_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(
            "Collapse(a, UnknownAsFalse) AND Collapse(b, UnknownAsTrue)"
        );

        Assert.Equal(2, result.Diagnostics.Count(d => d.Code == DiagnosticCodes.NestedCollapse));
    }

    /// <summary>The policy must be one of the three names; anything else is a readable syntax error at the policy token.</summary>
    [Theory]
    [InlineData("Collapse(a, Banana)", "Banana")]
    [InlineData("Collapse(a, True)", "True")]
    [InlineData("Collapse(a, \"UnknownAsFalse\")", "\"UnknownAsFalse\"")]
    public void Compile_WithAnInvalidPolicy_ReportsAReadableSyntaxErrorAtThePolicy_Test(string text, string offending)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Diagnostic error = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.SyntaxError);
        Assert.Contains("UnknownAsFalse", error.Message, StringComparison.Ordinal);
        Assert.Contains("UnknownAsTrue", error.Message, StringComparison.Ordinal);
        Assert.Contains("UnknownIsError", error.Message, StringComparison.Ordinal);
        Assert.Equal(offending, text.Substring(error.Span.Start, error.Span.Length));
    }

    /// <summary>A wrong argument count is a syntax error rather than a silent default policy.</summary>
    [Theory]
    [InlineData("Collapse(a)")]
    [InlineData("Collapse()")]
    [InlineData("Collapse(a, UnknownAsFalse, b)")]
    [InlineData("Collapse a")]
    public void Compile_WithTheWrongShape_ReportsASyntaxError_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.SyntaxError);
    }

    /// <summary>JSON writes the outermost collapse as <c>{"op": "collapse", "policy": ..., "operands": [x]}</c> and round-trips it.</summary>
    [Theory]
    [InlineData("Collapse(a AND NOT b, UnknownAsFalse)", "unknownAsFalse")]
    [InlineData("Collapse(a AND NOT b, UnknownAsTrue)", "unknownAsTrue")]
    [InlineData("Collapse(a AND NOT b, UnknownIsError)", "unknownIsError")]
    public void PrintJson_DeclaredCollapse_RoundTripsWithThePolicyField_Test(string text, string policyText)
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile(text).CompiledRule!;

        string json = original.PrintJson();
        CompiledRule<RuleTestContext> reparsed = Compiler.CompileJson(json).CompiledRule!;

        string compact = json.Replace(" ", string.Empty, StringComparison.Ordinal);
        Assert.StartsWith("{\"op\":\"collapse\"", compact, StringComparison.Ordinal);
        Assert.Contains($"\"policy\":\"{policyText}\"", compact, StringComparison.Ordinal);
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>The JSON policy is read in any letter case, so a hand-written document need not match the printer's casing.</summary>
    [Theory]
    [InlineData("unknownasfalse", CollapsePolicy.UnknownAsFalse)]
    [InlineData("UnknownAsTrue", CollapsePolicy.UnknownAsTrue)]
    [InlineData("UNKNOWNISERROR", CollapsePolicy.UnknownIsError)]
    public void CompileJson_PolicyInAnyLetterCase_IsAccepted_Test(string policyText, CollapsePolicy expected)
    {
        CompilationResult<RuleTestContext> result = Compiler.CompileJson(
            $$"""{"op": "Collapse", "policy": "{{policyText}}", "operands": [{"predicate": "a"}]}"""
        );

        Assert.True(result.Succeeded);
        Assert.Equal(expected, result.CompiledRule!.CollapsePolicy);
    }

    /// <summary>A missing or unrecognised JSON policy is a malformed tree.</summary>
    [Theory]
    [InlineData("""{"op": "collapse", "operands": [{"predicate": "a"}]}""")]
    [InlineData("""{"op": "collapse", "policy": "banana", "operands": [{"predicate": "a"}]}""")]
    [InlineData("""{"op": "collapse", "policy": 1, "operands": [{"predicate": "a"}]}""")]
    [InlineData("""{"op": "collapse", "policy": true, "operands": [{"predicate": "a"}]}""")]
    public void CompileJson_WithABadPolicy_ReportsMalformedTree_Test(string json)
    {
        CompilationResult<RuleTestContext> result = Compiler.CompileJson(json);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MalformedTree);
    }

    /// <summary>A JSON collapse node must have exactly one operand.</summary>
    [Fact]
    public void CompileJson_CollapseWithTwoOperands_ReportsMalformedTree_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.CompileJson(
            """{"op": "collapse", "policy": "unknownAsFalse", "operands": [{"predicate": "a"}, {"predicate": "b"}]}"""
        );

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MalformedTree);
    }

    /// <summary>JSON enforces the outermost-only rule too: a nested collapse node is the same diagnostic.</summary>
    [Fact]
    public void CompileJson_NestedCollapse_ReportsTheNestedCollapseDiagnostic_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.CompileJson(
            """
            {"op": "and", "operands": [
                {"predicate": "a"},
                {"op": "collapse", "policy": "unknownAsFalse", "operands": [{"predicate": "b"}]}]}
            """
        );

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.NestedCollapse);
    }

    /// <summary>YAML uses the same node shape and round-trips.</summary>
    [Theory]
    [InlineData("Collapse(a AND NOT b, UnknownAsFalse)", "policy: unknownAsFalse")]
    [InlineData("Collapse(a AND NOT b, UnknownIsError)", "policy: unknownIsError")]
    public void PrintYaml_DeclaredCollapse_RoundTripsWithThePolicyField_Test(string text, string expectedLine)
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile(text).CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<RuleTestContext> reparsed = Compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Contains("op: collapse", yaml, StringComparison.Ordinal);
        Assert.Contains(expectedLine, yaml, StringComparison.Ordinal);
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>YAML rejects a missing or unrecognised policy and a nested collapse.</summary>
    [Fact]
    public void CompileYaml_WithABadPolicy_ReportsMalformedTree_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.CompileYaml(
            "op: collapse\npolicy: banana\noperands:\n- predicate: a\n"
        );

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MalformedTree);
    }

    /// <summary>YAML enforces the outermost-only rule: a nested collapse node is the same diagnostic.</summary>
    [Fact]
    public void CompileYaml_NestedCollapse_ReportsTheNestedCollapseDiagnostic_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.CompileYaml(
            "op: not\noperands:\n- op: collapse\n  policy: unknownAsFalse\n  operands:\n  - predicate: a\n"
        );

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.NestedCollapse);
    }

    /// <summary>The builder produces the same rule as the DSL for every policy, and a nested one is rejected like in the DSL.</summary>
    [Theory]
    [InlineData(CollapsePolicy.UnknownAsFalse)]
    [InlineData(CollapsePolicy.UnknownAsTrue)]
    [InlineData(CollapsePolicy.UnknownIsError)]
    public void Collapse_Builder_CompilesToTheSameCanonicalTextAsDsl_Test(CollapsePolicy policy)
    {
        RuleBuilder builder = RuleBuilder.Collapse(
            RuleBuilder.And(RuleBuilder.Predicate("a"), RuleBuilder.Not(RuleBuilder.Predicate("b"))),
            policy
        );

        CompilationResult<RuleTestContext> result = builder.Compile(Compiler);

        Assert.True(result.Succeeded);
        Assert.Equal($"Collapse(a AND NOT b, {policy})", result.CompiledRule!.CanonicalText);
        Assert.Equal(policy, result.CompiledRule.CollapsePolicy);
    }

    /// <summary>A builder collapse used as an operand is rejected by the compiler with the nested-collapse diagnostic.</summary>
    [Fact]
    public void Collapse_BuilderNestedInAnOperator_ReportsTheNestedCollapseDiagnostic_Test()
    {
        RuleBuilder builder = RuleBuilder.And(
            RuleBuilder.Predicate("a"),
            RuleBuilder.Collapse(RuleBuilder.Predicate("b"), CollapsePolicy.UnknownAsFalse)
        );

        CompilationResult<RuleTestContext> result = builder.Compile(Compiler);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.NestedCollapse);
    }

    /// <summary>The analyzer analyzes the inner expression: a tautology under the collapse is still reported.</summary>
    [Theory]
    [InlineData("Collapse(a OR TRUE, UnknownAsFalse)", DiagnosticCodes.StructuralTautology)]
    [InlineData("Collapse(a AND FALSE, UnknownIsError)", DiagnosticCodes.StructuralContradiction)]
    public void Compile_CollapseOverAConstantInner_ReportsTheInnerFinding_Test(string text, string code)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.True(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == code);
    }

    /// <summary>The collapse adds no analyzer finding of its own, so a plain inner expression compiles clean.</summary>
    [Fact]
    public void Compile_CollapseOverAPlainInner_ReportsNoFinding_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile("Collapse(a AND b, UnknownAsFalse)");

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    /// <summary>The description tree has a root node naming the policy, wrapping the inner expression's own description.</summary>
    [Fact]
    public void Describe_DeclaredCollapse_WrapsTheInnerDescriptionInAPolicyRoot_Test()
    {
        RuleDescription description = Compiler.Compile("Collapse(a AND b, UnknownIsError)").CompiledRule!.Describe();

        Assert.Equal("Collapse(UnknownIsError)", description.Label);
        Assert.Contains("rejected", description.Description, StringComparison.OrdinalIgnoreCase);
        RuleDescription inner = Assert.Single(description.Operands);
        Assert.Equal("AND", inner.Label);
    }

    /// <summary>A rule without a collapse describes exactly the inner expression, as before.</summary>
    [Fact]
    public void Describe_WithoutACollapse_IsUnchanged_Test()
    {
        RuleDescription description = Compiler.Compile("a AND b").CompiledRule!.Describe();

        Assert.Equal("AND", description.Label);
    }

    /// <summary>The evaluated tree mirrors the description: a policy root whose single child is the inner result.</summary>
    [Fact]
    public async Task EvaluateAsync_DeclaredCollapse_WrapsTheEvaluatedTreeInAPolicyRoot_Test()
    {
        K3Rule rule = K3Rule.TryCreate("Collapse(a, UnknownAsTrue)", 1)!;

        Decision decision = await rule.EvaluateAsync([TruthValue.Unknown], TestContext.Current.CancellationToken);

        Assert.Equal("Collapse(UnknownAsTrue)", decision.EvaluatedTree!.NodeDescription);
        Assert.Equal(TruthValue.True, decision.EvaluatedTree.Result);
        EvaluatedNode inner = Assert.Single(decision.EvaluatedTree.Children);
        Assert.Equal(TruthValue.Unknown, inner.Result);
    }

    /// <summary>The rendered trees show the policy root in the plain-text and Mermaid renderings, with and without an evaluation.</summary>
    [Fact]
    public async Task Print_DeclaredCollapse_ShowsThePolicyRootInEveryRendering_Test()
    {
        CompiledRule<RuleTestContext> rule = Compiler.Compile("Collapse(a AND b, UnknownAsFalse)").CompiledRule!;
        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Contains("Collapse(UnknownAsFalse)", rule.PrintPlainText(), StringComparison.Ordinal);
        Assert.Contains("Collapse(UnknownAsFalse)", rule.PrintPlainText(decision), StringComparison.Ordinal);
        Assert.Contains("Collapse(UnknownAsFalse)", rule.PrintMermaid(), StringComparison.Ordinal);
        Assert.Contains("Collapse(UnknownAsFalse)", rule.PrintMermaid(decision), StringComparison.Ordinal);
    }

    /// <summary>The collapse has no symbolic or C-style spelling, so every tree style keeps the word.</summary>
    [Theory]
    [InlineData(OperatorStyle.Word)]
    [InlineData(OperatorStyle.Symbolic)]
    [InlineData(OperatorStyle.CStyle)]
    public void Print_DeclaredCollapse_KeepsTheWordInEveryStyle_Test(OperatorStyle style)
    {
        RuleDescription tree = Compiler.Compile("Collapse(a, UnknownAsFalse)").CompiledRule!.Describe();

        Assert.Contains("Collapse(UnknownAsFalse)", PlainTextTreePrinter.Print(tree, style), StringComparison.Ordinal);
        Assert.Contains("Collapse(UnknownAsFalse)", MermaidTreePrinter.Print(tree, style), StringComparison.Ordinal);
    }
}
