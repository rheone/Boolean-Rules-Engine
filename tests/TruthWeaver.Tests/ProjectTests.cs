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
/// <c>Project(expr, True|False)</c> keeps <c>True</c>/<c>False</c> and replaces <c>Unknown</c> with the chosen definite
/// value (ADR-0005 decision 12): the result is always definite and equals <c>COALESCE(expr, value)</c>.
/// </summary>
public sealed class ProjectTests
{
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .Build()
    );

    /// <summary>Both policies, in any letter case of keyword and value, match the oracle for each input value.</summary>
    [Theory]
    [InlineData("Project(a, True)", TruthValue.True)]
    [InlineData("project(a, TRUE)", TruthValue.True)]
    [InlineData("PROJECT(a, true)", TruthValue.True)]
    [InlineData("Project(a, False)", TruthValue.False)]
    [InlineData("project(a, FALSE)", TruthValue.False)]
    [InlineData("PROJECT(a, false)", TruthValue.False)]
    public async Task Evaluate_OverAllInputs_MatchesOracle_Test(string text, TruthValue unknownAs)
    {
        K3Rule rule = K3Rule.TryCreate(text, 1)!;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(1))
        {
            Decision actual = await rule.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(K3Oracle.Project(assignment[0], unknownAs), actual.Result);
        }
    }

    /// <summary>The projection is always definite, even over a composed operand with several Unknown terms.</summary>
    [Theory]
    [InlineData("Project(a AND b, True)")]
    [InlineData("Project(a XOR b, False)")]
    [InlineData("Project(a ?? b, True)")]
    [InlineData("Project(If(a, b, c), False)")]
    [InlineData("Project(Project(a, True), False)")]
    public async Task Evaluate_OverComposedOperands_NeverYieldsUnknown_Test(string text)
    {
        K3Rule rule = K3Rule.TryCreate(text, 3)!;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(3))
        {
            Decision actual = await rule.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.NotEqual(TruthValue.Unknown, actual.Result);
        }
    }

    /// <summary>Known values pass through unchanged whichever policy is chosen.</summary>
    [Theory]
    [InlineData("Project(a, True)", TruthValue.True, TruthValue.True)]
    [InlineData("Project(a, True)", TruthValue.False, TruthValue.False)]
    [InlineData("Project(a, False)", TruthValue.True, TruthValue.True)]
    [InlineData("Project(a, False)", TruthValue.False, TruthValue.False)]
    public async Task Evaluate_KnownOperand_PassesThroughUnchanged_Test(string text, TruthValue input, TruthValue expected)
    {
        K3Rule rule = K3Rule.TryCreate(text, 1)!;

        Decision decision = await rule.EvaluateAsync([input], TestContext.Current.CancellationToken);

        Assert.Equal(expected, decision.Result);
    }

    /// <summary><c>Unknown</c> becomes the chosen value and records no fault.</summary>
    [Theory]
    [InlineData("Project(a, True)", TruthValue.True)]
    [InlineData("Project(a, False)", TruthValue.False)]
    public async Task Evaluate_UnknownOperand_BecomesTheChosenValue_Test(string text, TruthValue expected)
    {
        K3Rule rule = K3Rule.TryCreate(text, 1)!;

        Decision decision = await rule.EvaluateAsync([TruthValue.Unknown], TestContext.Current.CancellationToken);

        Assert.Equal(expected, decision.Result);
        Assert.Empty(decision.Faults);
    }

    /// <summary><c>Project(x, v)</c> and <c>COALESCE(x, v)</c> agree for every input (ADR-0005 decision 12).</summary>
    [Theory]
    [InlineData("True")]
    [InlineData("False")]
    public async Task Evaluate_ComparedWithCoalesce_AgreesForEveryInput_Test(string value)
    {
        K3Rule project = K3Rule.TryCreate($"Project(a AND b, {value})", 2)!;
        K3Rule coalesce = K3Rule.TryCreate($"COALESCE(a AND b, {value})", 2)!;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(2))
        {
            Decision viaProject = await project.EvaluateAsync(assignment, TestContext.Current.CancellationToken);
            Decision viaCoalesce = await coalesce.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(viaCoalesce.Result, viaProject.Result);
        }
    }

    /// <summary>
    /// A faulting predicate is Unknown plus a Fault; the projection turns the value definite but the Fault is kept, so a
    /// caller can still tell "not known" from "broke".
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_ProjectingAFaultingPredicate_YieldsTheValueAndKeepsTheFault_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().AddThrowing("boom").AddConstant("ok", true).Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("Project(boom, True) AND ok").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
        Assert.Single(decision.Faults);
    }

    /// <summary>The evaluated tree labels the node with the operator and policy and has the operand as its only child.</summary>
    [Fact]
    public async Task EvaluateAsync_Project_ProducesALabelledNodeWithOneChild_Test()
    {
        K3Rule rule = K3Rule.TryCreate("Project(a, True)", 1)!;

        Decision decision = await rule.EvaluateAsync([TruthValue.Unknown], TestContext.Current.CancellationToken);

        Assert.Equal("Project(True)", decision.EvaluatedTree!.NodeDescription);
        Assert.Equal(TruthValue.True, decision.EvaluatedTree.Result);
        EvaluatedNode child = Assert.Single(decision.EvaluatedTree.Children);
        Assert.Equal(TruthValue.Unknown, child.Result);
    }

    /// <summary>Every spelling prints as the canonical function call with the value in canonical case.</summary>
    [Theory]
    [InlineData("project(a, true)", "Project(a, True)")]
    [InlineData("PROJECT( a , FALSE )", "Project(a, False)")]
    [InlineData("Project(a AND b, True)", "Project(a AND b, True)")]
    [InlineData("NOT Project(a, False)", "NOT Project(a, False)")]
    [InlineData("Project(a, True) AND Project(b, False)", "Project(a, True) AND Project(b, False)")]
    [InlineData("Project(Project(a, True), False)", "Project(Project(a, True), False)")]
    [InlineData("Project(a XOR b, True)", "Project((a XOR b), True)")]
    public void Compile_AnySpelling_ProducesTheCanonicalText_Test(string text, string expected)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.True(result.Succeeded);
        Assert.Equal(expected, result.CompiledRule!.CanonicalText);
    }

    /// <summary><c>Project</c> is reserved so a predicate cannot shadow it.</summary>
    [Theory]
    [InlineData("project")]
    [InlineData("Project")]
    [InlineData("PROJECT")]
    public void IsReservedWord_Project_IsReserved_Test(string word)
    {
        Assert.True(DslParser.IsReservedWord(word));
    }

    /// <summary>The value must be the constant True or False; Unknown is rejected with a readable message.</summary>
    [Fact]
    public void Compile_WithUnknownAsTheValue_ReportsAReadableSyntaxErrorAtTheValue_Test()
    {
        const string text = "Project(a, Unknown)";

        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Diagnostic error = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.SyntaxError);
        Assert.Contains("Unknown", error.Message, StringComparison.Ordinal);
        Assert.Contains("True or False", error.Message, StringComparison.Ordinal);
        Assert.Equal("Unknown", text.Substring(error.Span.Start, error.Span.Length));
    }

    /// <summary>A term or any other expression as the value is not a constant and is rejected at its own span.</summary>
    [Theory]
    [InlineData("Project(a, b)", "b")]
    [InlineData("Project(a, NOT True)", "NOT True")]
    [InlineData("Project(a, b AND c)", "b AND c")]
    public void Compile_WithANonConstantValue_ReportsAReadableSyntaxErrorAtTheValue_Test(string text, string offending)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Diagnostic error = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.SyntaxError);
        Assert.Contains("constant True or False", error.Message, StringComparison.Ordinal);
        Assert.Equal(offending, text.Substring(error.Span.Start, error.Span.Length));
    }

    /// <summary>A wrong argument count is a syntax error rather than a silent default.</summary>
    [Theory]
    [InlineData("Project(a)")]
    [InlineData("Project()")]
    [InlineData("Project(a, True, False)")]
    [InlineData("Project(True, a, b)")]
    public void Compile_WithTheWrongArgumentCount_ReportsASyntaxError_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.SyntaxError);
    }

    /// <summary>A bare keyword without a call is not an expression.</summary>
    [Fact]
    public void Compile_ProjectWithoutParentheses_ReportsSyntaxError_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile("Project a");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.SyntaxError);
    }

    /// <summary>JSON carries the policy as a boolean <c>unknownAs</c>, round-trips, and reads the op in any letter case.</summary>
    [Theory]
    [InlineData("Project(a AND NOT b, True)", "true")]
    [InlineData("Project(a AND NOT b, False)", "false")]
    public void PrintJson_Project_RoundTripsWithTheUnknownAsField_Test(string text, string expectedValue)
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile(text).CompiledRule!;

        string json = original.PrintJson();
        CompiledRule<RuleTestContext> reparsed = Compiler
            .CompileJson(json.Replace("project", "PROJECT", StringComparison.Ordinal))
            .CompiledRule!;

        string compact = json.Replace(" ", string.Empty, StringComparison.Ordinal);
        Assert.Contains("\"op\":\"project\"", compact, StringComparison.Ordinal);
        Assert.Contains($"\"unknownAs\":{expectedValue}", compact, StringComparison.Ordinal);
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>YAML uses the same node shape and round-trips.</summary>
    [Theory]
    [InlineData("Project(a AND NOT b, True)", "unknownAs: true")]
    [InlineData("Project(a AND NOT b, False)", "unknownAs: false")]
    public void PrintYaml_Project_RoundTripsWithTheUnknownAsField_Test(string text, string expectedLine)
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile(text).CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<RuleTestContext> reparsed = Compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Contains("op: project", yaml, StringComparison.Ordinal);
        Assert.Contains(expectedLine, yaml, StringComparison.Ordinal);
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>JSON also accepts the policy as the string <c>"true"</c>/<c>"false"</c> in any letter case, like <c>const</c>.</summary>
    [Theory]
    [InlineData("\"true\"", "Project(a, True)")]
    [InlineData("\"FALSE\"", "Project(a, False)")]
    [InlineData("true", "Project(a, True)")]
    public void CompileJson_UnknownAsInEitherForm_IsAccepted_Test(string value, string expected)
    {
        CompilationResult<RuleTestContext> result = Compiler.CompileJson(
            $$"""{"op": "project", "unknownAs": {{value}}, "operands": [{"predicate": "a"}]}"""
        );

        Assert.True(result.Succeeded);
        Assert.Equal(expected, result.CompiledRule!.CanonicalText);
    }

    /// <summary>A missing, <c>unknown</c>-valued or non-boolean <c>unknownAs</c> is a malformed tree.</summary>
    [Theory]
    [InlineData("""{"op": "project", "operands": [{"predicate": "a"}]}""")]
    [InlineData("""{"op": "project", "unknownAs": "unknown", "operands": [{"predicate": "a"}]}""")]
    [InlineData("""{"op": "project", "unknownAs": 1, "operands": [{"predicate": "a"}]}""")]
    [InlineData("""{"op": "project", "unknownAs": null, "operands": [{"predicate": "a"}]}""")]
    public void CompileJson_WithABadUnknownAs_ReportsMalformedTree_Test(string json)
    {
        CompilationResult<RuleTestContext> result = Compiler.CompileJson(json);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MalformedTree);
    }

    /// <summary>YAML rejects the same bad <c>unknownAs</c> values.</summary>
    [Theory]
    [InlineData("op: project\noperands:\n- predicate: a\n")]
    [InlineData("op: project\nunknownAs: unknown\noperands:\n- predicate: a\n")]
    public void CompileYaml_WithABadUnknownAs_ReportsMalformedTree_Test(string yaml)
    {
        CompilationResult<RuleTestContext> result = Compiler.CompileYaml(yaml);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MalformedTree);
    }

    /// <summary>A JSON project node with the wrong operand count is rejected by the compiler.</summary>
    [Fact]
    public void CompileJson_ProjectWithTwoOperands_ReportsMalformedTree_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.CompileJson(
            """{"op": "project", "unknownAs": true, "operands": [{"predicate": "a"}, {"predicate": "b"}]}"""
        );

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.MalformedTree);
    }

    /// <summary>The builder produces the same rule as the DSL for both policies.</summary>
    [Fact]
    public void Project_Builder_CompilesToTheSameCanonicalTextAsDsl_Test()
    {
        RuleBuilder builder = RuleBuilder.And(
            RuleBuilder.Project(RuleBuilder.Predicate("a"), unknownAs: true),
            RuleBuilder.Project(RuleBuilder.Not(RuleBuilder.Predicate("b")), unknownAs: false)
        );

        CompilationResult<RuleTestContext> result = builder.Compile(Compiler);

        Assert.True(result.Succeeded);
        Assert.Equal("Project(a, True) AND Project(NOT b, False)", result.CompiledRule!.CanonicalText);
    }

    /// <summary>The description names the policy and says the result is definite.</summary>
    [Theory]
    [InlineData("Project(a, True)", "Project(True)")]
    [InlineData("Project(a, False)", "Project(False)")]
    public void Describe_Project_ExplainsItsDefiniteResult_Test(string text, string label)
    {
        RuleDescription description = Compiler.Compile(text).CompiledRule!.Describe();

        Assert.Equal(label, description.Label);
        Assert.Contains("never Unknown", description.Description, StringComparison.Ordinal);
        Assert.Equal(["a"], description.Operands.Select(o => o.Label));
    }

    /// <summary><c>Project</c> has no symbolic or C-style spelling, so every tree style keeps the word.</summary>
    [Theory]
    [InlineData(OperatorStyle.Word)]
    [InlineData(OperatorStyle.Symbolic)]
    [InlineData(OperatorStyle.CStyle)]
    public void Print_Project_KeepsTheWordInEveryStyle_Test(OperatorStyle style)
    {
        RuleDescription tree = Compiler.Compile("Project(a, True)").CompiledRule!.Describe();

        Assert.Contains("Project(True)", PlainTextTreePrinter.Print(tree, style), StringComparison.Ordinal);
        Assert.Contains("Project(True)", MermaidTreePrinter.Print(tree, style), StringComparison.Ordinal);
    }

    /// <summary>
    /// The analyzer knows a projection is always definite: <c>Project(a, True) OR NOT Project(a, True)</c> is a genuine
    /// tautology (a classical one, since the operand is definite), and its conjunction a genuine contradiction.
    /// </summary>
    [Theory]
    [InlineData("Project(a, True) OR NOT Project(a, True)", DiagnosticCodes.StructuralTautology)]
    [InlineData("Project(a, False) OR NOT Project(a, False)", DiagnosticCodes.StructuralTautology)]
    [InlineData("Project(a, True) AND NOT Project(a, True)", DiagnosticCodes.StructuralContradiction)]
    [InlineData("Project(a, False) AND NOT Project(a, False)", DiagnosticCodes.StructuralContradiction)]
    public void Compile_ProjectionsOfTheSameTerm_ReportsTheK3Verdict_Test(string text, string code)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.True(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == code);
    }

    /// <summary>A projection of an ordinary term is neither a tautology nor a contradiction.</summary>
    [Theory]
    [InlineData("Project(a, True)")]
    [InlineData("Project(a, False)")]
    [InlineData("Project(a, True) AND Project(b, False)")]
    public void Compile_PlainProjection_ReportsNoStructuralFinding_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.True(result.Succeeded);
        Assert.DoesNotContain(
            result.Diagnostics,
            d => d.Code is DiagnosticCodes.StructuralTautology or DiagnosticCodes.StructuralContradiction
        );
    }
}
