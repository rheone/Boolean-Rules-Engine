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
/// <c>NAND</c> / <c>↑</c> (negated conjunction) and <c>NOR</c> / <c>↓</c> (negated disjunction) are binary,
/// first-class tree nodes (ADR-0005 decisions 3 and 3a) that every front end and printer supports.
/// </summary>
public sealed class NandNorTests
{
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .Build()
    );

    /// <summary>Word, symbol and mixed-case spellings of NAND compile to the same canonical text.</summary>
    [Theory]
    [InlineData("a NAND b")]
    [InlineData("a nand b")]
    [InlineData("a Nand b")]
    [InlineData("a ↑ b")]
    [InlineData("(a) ↑ (b)")]
    public void Compile_AnyNandSpelling_ProducesTheSameCanonicalText_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.True(result.Succeeded);
        Assert.Equal("(a NAND b)", result.CompiledRule!.CanonicalText);
    }

    /// <summary>Word, symbol and mixed-case spellings of NOR compile to the same canonical text.</summary>
    [Theory]
    [InlineData("a NOR b")]
    [InlineData("a nor b")]
    [InlineData("a Nor b")]
    [InlineData("a ↓ b")]
    [InlineData("(a) ↓ (b)")]
    public void Compile_AnyNorSpelling_ProducesTheSameCanonicalText_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.True(result.Succeeded);
        Assert.Equal("(a NOR b)", result.CompiledRule!.CanonicalText);
    }

    /// <summary>NAND, in word and symbol spelling, matches the oracle's <c>NOT AND</c> for every assignment.</summary>
    [Theory]
    [InlineData("a NAND b")]
    [InlineData("a ↑ b")]
    public async Task Evaluate_NandOverAllAssignments_MatchesOracle_Test(string text)
    {
        K3Rule rule = K3Rule.TryCreate(text, 2)!;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(2))
        {
            Decision actual = await rule.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(K3Oracle.Nand(assignment[0], assignment[1]), actual.Result);
        }
    }

    /// <summary>NOR, in word and symbol spelling, matches the oracle's <c>NOT OR</c> for every assignment.</summary>
    [Theory]
    [InlineData("a NOR b")]
    [InlineData("a ↓ b")]
    public async Task Evaluate_NorOverAllAssignments_MatchesOracle_Test(string text)
    {
        K3Rule rule = K3Rule.TryCreate(text, 2)!;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(2))
        {
            Decision actual = await rule.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(K3Oracle.Nor(assignment[0], assignment[1]), actual.Result);
        }
    }

    /// <summary>Spot-check the K3 table: a False operand makes NAND True, and a True operand makes NOR False, even next to Unknown.</summary>
    [Theory]
    [InlineData("a NAND b", TruthValue.False, TruthValue.Unknown, TruthValue.True)]
    [InlineData("a NAND b", TruthValue.True, TruthValue.Unknown, TruthValue.Unknown)]
    [InlineData("a NOR b", TruthValue.True, TruthValue.Unknown, TruthValue.False)]
    [InlineData("a NOR b", TruthValue.False, TruthValue.Unknown, TruthValue.Unknown)]
    public async Task Evaluate_NandNorWithUnknown_FollowsStrongKleeneDominance_Test(
        string text,
        TruthValue left,
        TruthValue right,
        TruthValue expected
    )
    {
        K3Rule rule = K3Rule.TryCreate(text, 2)!;

        Decision decision = await rule.EvaluateAsync([left, right], TestContext.Current.CancellationToken);

        Assert.Equal(expected, decision.Result);
    }

    /// <summary>NAND and NOR follow the no-mixing rule: no unparenthesized mixing with AND/OR or another infix operator.</summary>
    [Theory]
    [InlineData("a NAND b AND c")]
    [InlineData("a AND b NAND c")]
    [InlineData("a OR b ↓ c")]
    [InlineData("a NOR b XOR c")]
    [InlineData("a NAND b NOR c")]
    [InlineData("a ↑ b → c")]
    [InlineData("a EQUIVALENT b ↓ c")]
    public void Compile_NandNorMixedWithAnotherOperatorWithoutParentheses_ReportsAmbiguousMixing_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.AmbiguousOperatorMixing);
    }

    /// <summary>The mixing diagnostic names the canonical operators involved.</summary>
    [Fact]
    public void Compile_NandThenNor_NamesBothOperators_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile("a ↑ b ↓ c");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.AmbiguousOperatorMixing);
        Assert.Contains("Mixing NAND with NOR", diagnostic.Message, StringComparison.Ordinal);
    }

    /// <summary>Explicit parentheses make NAND/NOR combine with anything.</summary>
    [Theory]
    [InlineData("(a NAND b) AND c")]
    [InlineData("a OR (b ↓ c)")]
    [InlineData("(a ↑ b) NOR c")]
    [InlineData("ExactlyOne(a NAND b, c)")]
    public void Compile_NandNorInsideParentheses_Compiles_Test(string text)
    {
        Assert.True(Compiler.Compile(text).Succeeded);
    }

    /// <summary>NAND and NOR are binary: a chain is rejected with the infix arity code and a parentheses hint.</summary>
    [Theory]
    [InlineData("a NAND b NAND c", "NAND is binary")]
    [InlineData("a ↓ b ↓ c", "NOR is binary")]
    public void Compile_ChainedNandNor_ReportsArityViolationWithParenthesesHint_Test(string text, string expected)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.InfixArityViolation, diagnostic.Code);
        Assert.Contains(expected, diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("parentheses", diagnostic.Message, StringComparison.Ordinal);
    }

    /// <summary>The keywords are reserved so a predicate cannot shadow them.</summary>
    [Theory]
    [InlineData("nand")]
    [InlineData("NOR")]
    public void IsReservedWord_NandNor_IsReserved_Test(string word)
    {
        Assert.True(DslParser.IsReservedWord(word));
    }

    /// <summary>JSON uses <c>nand</c>/<c>nor</c> ops, round-trips, and reads the op name in any letter case.</summary>
    [Theory]
    [InlineData("a NAND b", "nand")]
    [InlineData("a NOR b", "nor")]
    public void PrintJson_NandNor_RoundTripsWithTheOpName_Test(string text, string op)
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile(text).CompiledRule!;

        string json = original.PrintJson();
        CompiledRule<RuleTestContext> reparsed = Compiler.CompileJson(json).CompiledRule!;

        Assert.Contains(
            $"\"op\":\"{op}\"",
            json.Replace(" ", string.Empty, StringComparison.Ordinal),
            StringComparison.Ordinal
        );
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>A JSON NAND with the wrong operand count is a compile error.</summary>
    [Theory]
    [InlineData("nand")]
    [InlineData("NOR")]
    public void CompileJson_WithThreeOperands_ReportsArityViolation_Test(string op)
    {
        string json = $$"""{"op": "{{op}}", "operands": [{"predicate": "a"}, {"predicate": "b"}, {"predicate": "c"}]}""";

        CompilationResult<RuleTestContext> result = Compiler.CompileJson(json);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.InfixArityViolation);
    }

    /// <summary>YAML uses the same node shape and round-trips.</summary>
    [Theory]
    [InlineData("a NAND b", "nand")]
    [InlineData("a NOR b", "nor")]
    public void PrintYaml_NandNor_RoundTripsWithTheOpName_Test(string text, string op)
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile(text).CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<RuleTestContext> reparsed = Compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Contains($"op: {op}", yaml, StringComparison.Ordinal);
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>The builder produces the same rules as the DSL.</summary>
    [Fact]
    public void NandNor_Builders_CompileToTheSameCanonicalTextAsDsl_Test()
    {
        CompiledRule<RuleTestContext> nand = RuleBuilder
            .Nand(RuleBuilder.Predicate("a"), RuleBuilder.Not(RuleBuilder.Predicate("b")))
            .Compile(Compiler)
            .CompiledRule!;
        CompiledRule<RuleTestContext> nor = RuleBuilder
            .Nor(RuleBuilder.Predicate("a"), RuleBuilder.Predicate("b"))
            .Compile(Compiler)
            .CompiledRule!;

        Assert.Equal("(a NAND NOT b)", nand.CanonicalText);
        Assert.Equal("(a NOR b)", nor.CanonicalText);
    }

    /// <summary>The evaluated tree labels the nodes NAND/NOR and keeps operand order.</summary>
    [Theory]
    [InlineData("a NAND b", "NAND")]
    [InlineData("a NOR b", "NOR")]
    public async Task EvaluateAsync_NandNor_LabelsTheNodeAndKeepsOperandOrder_Test(string text, string label)
    {
        K3Rule rule = K3Rule.TryCreate(text, 2)!;

        Decision decision = await rule.EvaluateAsync(
            [TruthValue.True, TruthValue.False],
            TestContext.Current.CancellationToken
        );

        Assert.Equal(label, decision.EvaluatedTree!.NodeDescription);
        Assert.Equal(["a", "b"], decision.EvaluatedTree.Children.Select(c => c.NodeDescription));
    }

    /// <summary>The operator descriptions explain the negated primitive each is defined by.</summary>
    [Theory]
    [InlineData("a NAND b", "NAND", "AND")]
    [InlineData("a NOR b", "NOR", "OR")]
    public void Describe_NandNor_ExplainsTheNegatedPrimitive_Test(string text, string label, string primitive)
    {
        RuleDescription description = Compiler.Compile(text).CompiledRule!.Describe();

        Assert.Equal(label, description.Label);
        Assert.Contains($"NOT (left {primitive} right)", description.Description, StringComparison.Ordinal);
        Assert.Equal(["a", "b"], description.Operands.Select(o => o.Label));
    }

    /// <summary>Tree renderings use the arrows in the symbolic style and the word otherwise (there is no C-family spelling).</summary>
    [Theory]
    [InlineData(OperatorStyle.Word, "NAND", "NOR")]
    [InlineData(OperatorStyle.Symbolic, "↑", "↓")]
    [InlineData(OperatorStyle.CStyle, "NAND", "NOR")]
    public void Print_NandNor_RendersTheStyleSpecificOperator_Test(OperatorStyle style, string nand, string nor)
    {
        RuleDescription nandTree = Compiler.Compile("a NAND b").CompiledRule!.Describe();
        RuleDescription norTree = Compiler.Compile("a NOR b").CompiledRule!.Describe();

        Assert.Contains(nand, PlainTextTreePrinter.Print(nandTree, style), StringComparison.Ordinal);
        Assert.Contains(nand, MermaidTreePrinter.Print(nandTree, style), StringComparison.Ordinal);
        Assert.Contains(nor, PlainTextTreePrinter.Print(norTree, style), StringComparison.Ordinal);
        Assert.Contains(nor, MermaidTreePrinter.Print(norTree, style), StringComparison.Ordinal);
    }
}
