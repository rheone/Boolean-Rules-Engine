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
/// <c>EQUIVALENT</c> / <c>↔</c> is the Strong Kleene biconditional (ADR-0005 decision 5). <c>IFF</c> and the legacy
/// <c>XNOR</c> are input aliases that produce the same node, so persisted rules keep compiling.
/// </summary>
public sealed class EquivalentTests
{
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .Build()
    );

    /// <summary>Every spelling (word, alias, legacy name, symbol, any case) compiles to the same canonical text.</summary>
    [Theory]
    [InlineData("a EQUIVALENT b")]
    [InlineData("a equivalent b")]
    [InlineData("a IFF b")]
    [InlineData("a iff b")]
    [InlineData("a XNOR b")]
    [InlineData("a xnor b")]
    [InlineData("a ↔ b")]
    [InlineData("(a) ↔ (b)")]
    public void Compile_AnyEquivalentSpelling_ProducesTheSameCanonicalText_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.True(result.Succeeded);
        Assert.Equal("(a EQUIVALENT b)", result.CompiledRule!.CanonicalText);
    }

    /// <summary>Every spelling matches the oracle's biconditional over all nine assignments.</summary>
    [Theory]
    [InlineData("a EQUIVALENT b")]
    [InlineData("a IFF b")]
    [InlineData("a XNOR b")]
    [InlineData("a ↔ b")]
    public async Task Evaluate_EquivalentOverAllAssignments_MatchesOracle_Test(string text)
    {
        K3Rule rule = K3Rule.TryCreate(text, 2)!;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(2))
        {
            Decision actual = await rule.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(K3Oracle.Equivalent(assignment[0], assignment[1]), actual.Result);
        }
    }

    /// <summary>EQUIVALENT follows the no-mixing rule like every other infix operator, in every spelling.</summary>
    [Theory]
    [InlineData("a EQUIVALENT b AND c")]
    [InlineData("a OR b ↔ c")]
    [InlineData("a IFF b XOR c")]
    [InlineData("a XNOR b → c")]
    public void Compile_EquivalentMixedWithAnotherOperatorWithoutParentheses_ReportsAmbiguousMixing_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.AmbiguousOperatorMixing);
    }

    /// <summary>A chain is a binary-arity error whose message names the canonical operator, reusing the infix arity code.</summary>
    [Theory]
    [InlineData("a EQUIVALENT b EQUIVALENT c")]
    [InlineData("a XNOR b XNOR c")]
    public void Compile_ChainedEquivalent_ReportsArityViolationNamingEquivalent_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.XorArityViolation, diagnostic.Code);
        Assert.Contains("EQUIVALENT is binary", diagnostic.Message, StringComparison.Ordinal);
    }

    /// <summary>The new keyword and both aliases are reserved so a predicate cannot shadow them.</summary>
    [Theory]
    [InlineData("equivalent")]
    [InlineData("iff")]
    [InlineData("xnor")]
    public void IsReservedWord_EquivalentFamily_IsReserved_Test(string word)
    {
        Assert.True(DslParser.IsReservedWord(word));
    }

    /// <summary>JSON prints the canonical <c>equivalent</c> op and round-trips.</summary>
    [Fact]
    public void PrintJson_Equivalent_UsesTheEquivalentOpNameAndRoundTrips_Test()
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile("a XNOR b").CompiledRule!;

        string json = original.PrintJson();
        CompiledRule<RuleTestContext> reparsed = Compiler.CompileJson(json).CompiledRule!;

        Assert.Contains(
            "\"op\":\"equivalent\"",
            json.Replace(" ", string.Empty, StringComparison.Ordinal),
            StringComparison.Ordinal
        );
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>Persisted JSON that uses the legacy <c>xnor</c> op (or the <c>iff</c> alias) still compiles to the same rule.</summary>
    [Theory]
    [InlineData("xnor")]
    [InlineData("XNOR")]
    [InlineData("iff")]
    [InlineData("equivalent")]
    public void CompileJson_LegacyOrAliasOp_CompilesToEquivalent_Test(string op)
    {
        string json = $$"""{"op": "{{op}}", "operands": [{"predicate": "a"}, {"predicate": "b"}]}""";

        CompilationResult<RuleTestContext> result = Compiler.CompileJson(json);

        Assert.Equal("(a EQUIVALENT b)", result.CompiledRule!.CanonicalText);
    }

    /// <summary>Legacy and alias ops keep the binary-arity check in JSON.</summary>
    [Theory]
    [InlineData("xnor")]
    [InlineData("iff")]
    public void CompileJson_AliasWithThreeOperands_ReportsArityViolation_Test(string op)
    {
        string json = $$"""{"op": "{{op}}", "operands": [{"predicate": "a"}, {"predicate": "b"}, {"predicate": "c"}]}""";

        CompilationResult<RuleTestContext> result = Compiler.CompileJson(json);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.XorArityViolation);
    }

    /// <summary>YAML prints the canonical <c>equivalent</c> op and round-trips.</summary>
    [Fact]
    public void PrintYaml_Equivalent_UsesTheEquivalentOpNameAndRoundTrips_Test()
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile("a IFF b").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<RuleTestContext> reparsed = Compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Contains("op: equivalent", yaml, StringComparison.Ordinal);
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>Persisted YAML using the legacy op names keeps compiling.</summary>
    [Theory]
    [InlineData("xnor")]
    [InlineData("iff")]
    public void CompileYaml_LegacyOrAliasOp_CompilesToEquivalent_Test(string op)
    {
        string yaml = $"op: {op}\noperands:\n  - predicate: a\n  - predicate: b\n";

        CompilationResult<RuleTestContext> result = Compiler.CompileYaml(yaml);

        Assert.Equal("(a EQUIVALENT b)", result.CompiledRule!.CanonicalText);
    }

    /// <summary>Both the new builder member and the legacy <c>Xnor</c> forwarding member produce the canonical rule.</summary>
    [Fact]
    public void Equivalent_BuilderAndLegacyXnorBuilder_CompileToTheSameCanonicalText_Test()
    {
        CompiledRule<RuleTestContext> viaEquivalent = RuleBuilder
            .Equivalent(RuleBuilder.Predicate("a"), RuleBuilder.Predicate("b"))
            .Compile(Compiler)
            .CompiledRule!;
        CompiledRule<RuleTestContext> viaXnor = RuleBuilder
            .Xnor(RuleBuilder.Predicate("a"), RuleBuilder.Predicate("b"))
            .Compile(Compiler)
            .CompiledRule!;

        Assert.Equal("(a EQUIVALENT b)", viaEquivalent.CanonicalText);
        Assert.Equal(viaEquivalent.CanonicalText, viaXnor.CanonicalText);
    }

    /// <summary>The evaluated tree and the description label the node EQUIVALENT.</summary>
    [Fact]
    public async Task EvaluateAsync_Equivalent_LabelsTheNodeEquivalent_Test()
    {
        K3Rule rule = K3Rule.TryCreate("a XNOR b", 2)!;

        Decision decision = await rule.EvaluateAsync([TruthValue.True, TruthValue.True], TestContext.Current.CancellationToken);

        Assert.Equal("EQUIVALENT", decision.EvaluatedTree!.NodeDescription);
        Assert.Equal("EQUIVALENT", Compiler.Compile("a IFF b").CompiledRule!.Describe().Label);
    }

    /// <summary>Tree renderings show the word, <c>↔</c> and <c>==</c> for the three operator styles.</summary>
    [Theory]
    [InlineData(OperatorStyle.Word, "EQUIVALENT")]
    [InlineData(OperatorStyle.Symbolic, "↔")]
    [InlineData(OperatorStyle.CStyle, "==")]
    public void Print_Equivalent_RendersTheStyleSpecificOperator_Test(OperatorStyle style, string expected)
    {
        CompiledRule<RuleTestContext> rule = Compiler.Compile("a EQUIVALENT b").CompiledRule!;

        Assert.Contains(expected, PlainTextTreePrinter.Print(rule.Describe(), style), StringComparison.Ordinal);
        Assert.Contains(expected, MermaidTreePrinter.Print(rule.Describe(), style), StringComparison.Ordinal);
    }
}
