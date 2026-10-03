namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Diffing;
using TruthWeaver.Evaluation;
using TruthWeaver.Parsing;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// <c>IMPLIES</c> / <c>→</c> is Strong Kleene material implication (<c>NOT A OR B</c>), a first-class
/// binary tree node (ADR-0005 decisions 3 and 3a) that every front end and printer supports.
/// </summary>
public sealed class ImpliesTests
{
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .Build()
    );

    /// <summary>Word, symbol and mixed-case spellings compile to the same canonical text.</summary>
    [Theory]
    [InlineData("a IMPLIES b")]
    [InlineData("a implies b")]
    [InlineData("a Implies b")]
    [InlineData("a → b")]
    [InlineData("(a) → (b)")]
    public void Compile_AnyImpliesSpelling_ProducesTheSameCanonicalText_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.True(result.Succeeded);
        Assert.Equal("(a IMPLIES b)", result.CompiledRule!.CanonicalText);
    }

    /// <summary>NOT binds tighter than IMPLIES, so a negated operand needs no parentheses.</summary>
    [Fact]
    public async Task Evaluate_NotOperandOfImplies_BindsTighterThanImplies_Test()
    {
        K3Rule bare = K3Rule.TryCreate("NOT a IMPLIES !b", 2)!;

        foreach (TruthValue[] assignment in K3Oracle.Assignments(2))
        {
            Decision actual = await bare.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

            Assert.Equal(K3Oracle.Implies(K3Oracle.Not(assignment[0]), K3Oracle.Not(assignment[1])), actual.Result);
        }
    }

    /// <summary>IMPLIES follows the no-mixing rule: it may not sit unparenthesized next to AND/OR or another infix operator.</summary>
    [Theory]
    [InlineData("a IMPLIES b AND c")]
    [InlineData("a AND b IMPLIES c")]
    [InlineData("a OR b → c")]
    [InlineData("a IMPLIES b XOR c")]
    [InlineData("a XNOR b → c")]
    public void Compile_ImpliesMixedWithAnotherOperatorWithoutParentheses_ReportsAmbiguousMixing_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.AmbiguousOperatorMixing);
    }

    /// <summary>The mixing diagnostic points at the second operator token (here the one-character XOR symbol).</summary>
    [Fact]
    public void Compile_ImpliesThenXor_ReportsAtTheXorToken_Test()
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile("a → b ⊕ c");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.AmbiguousOperatorMixing);
        Assert.Equal(new SourceSpan(6, 1), diagnostic.Span);
    }

    /// <summary>Explicit parentheses make IMPLIES combine with anything.</summary>
    [Theory]
    [InlineData("(a IMPLIES b) AND c")]
    [InlineData("a OR (b → c)")]
    [InlineData("(a → b) → c")]
    [InlineData("a → (b XOR c)")]
    [InlineData("ExactlyOne(a → b, c)")]
    public void Compile_ImpliesInsideParentheses_Compiles_Test(string text)
    {
        Assert.True(Compiler.Compile(text).Succeeded);
    }

    /// <summary>IMPLIES is binary: a chain is rejected rather than silently given an associativity.</summary>
    [Theory]
    [InlineData("a IMPLIES b IMPLIES c")]
    [InlineData("a → b → c")]
    public void Compile_ChainedImplies_ReportsArityViolationWithParenthesesHint_Test(string text)
    {
        CompilationResult<RuleTestContext> result = Compiler.Compile(text);

        Assert.False(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.XorArityViolation, diagnostic.Code);
        Assert.Contains("IMPLIES is binary", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("parentheses", diagnostic.Message, StringComparison.Ordinal);
    }

    /// <summary>A predicate may not be named after the keyword.</summary>
    [Fact]
    public void IsReservedWord_Implies_IsReserved_Test()
    {
        Assert.True(DslParser.IsReservedWord("implies"));
    }

    /// <summary>JSON uses <c>{"op": "implies", "operands": [antecedent, consequent]}</c> and round-trips.</summary>
    [Fact]
    public void PrintJson_Implies_RoundTripsWithTheImpliesOpName_Test()
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile("a → b").CompiledRule!;

        string json = original.PrintJson();
        CompiledRule<RuleTestContext> reparsed = Compiler.CompileJson(json).CompiledRule!;

        Assert.Contains(
            "\"op\":\"implies\"",
            json.Replace(" ", string.Empty, StringComparison.Ordinal),
            StringComparison.Ordinal
        );
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>The JSON op name is accepted in any letter case.</summary>
    [Fact]
    public void CompileJson_ImpliesOpInUpperCase_Compiles_Test()
    {
        const string json = """{"op": "IMPLIES", "operands": [{"predicate": "a"}, {"predicate": "b"}]}""";

        CompilationResult<RuleTestContext> result = Compiler.CompileJson(json);

        Assert.Equal("(a IMPLIES b)", result.CompiledRule!.CanonicalText);
    }

    /// <summary>YAML uses the same node shape and round-trips.</summary>
    [Fact]
    public void PrintYaml_Implies_RoundTripsWithTheImpliesOpName_Test()
    {
        CompiledRule<RuleTestContext> original = Compiler.Compile("a → b").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<RuleTestContext> reparsed = Compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Contains("op: implies", yaml, StringComparison.Ordinal);
        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>An IMPLIES node with the wrong operand count in JSON is a compile error.</summary>
    [Fact]
    public void CompileJson_ImpliesWithOneOperand_ReportsArityViolation_Test()
    {
        const string json = """{"op": "implies", "operands": [{"predicate": "a"}]}""";

        CompilationResult<RuleTestContext> result = Compiler.CompileJson(json);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.XorArityViolation);
    }

    /// <summary>The builder produces the same rule as the DSL.</summary>
    [Fact]
    public void Implies_Builder_CompilesToTheSameCanonicalTextAsDsl_Test()
    {
        CompiledRule<RuleTestContext> viaBuilder = RuleBuilder
            .Implies(RuleBuilder.Predicate("a"), RuleBuilder.Not(RuleBuilder.Predicate("b")))
            .Compile(Compiler)
            .CompiledRule!;

        Assert.Equal("(a IMPLIES NOT b)", viaBuilder.CanonicalText);
    }

    /// <summary>The evaluated tree labels the node IMPLIES and keeps antecedent-then-consequent order.</summary>
    [Fact]
    public async Task EvaluateAsync_Implies_LabelsTheNodeAndKeepsOperandOrder_Test()
    {
        K3Rule rule = K3Rule.TryCreate("a IMPLIES b", 2)!;

        Decision decision = await rule.EvaluateAsync(
            [TruthValue.True, TruthValue.False],
            TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.False, decision.Result);
        Assert.Equal("IMPLIES", decision.EvaluatedTree!.NodeDescription);
        Assert.Equal(["a", "b"], decision.EvaluatedTree.Children.Select(c => c.NodeDescription));
    }

    /// <summary>A skipped IMPLIES subtree (short-circuited by a sibling) is still described as IMPLIES.</summary>
    [Fact]
    public async Task EvaluateAsync_SkippedImpliesSubtree_IsDescribedAsImplies_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", false)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build()
        );
        CompiledRule<RuleTestContext> rule = RuleBuilder
            .And(RuleBuilder.Predicate("a"), RuleBuilder.Implies(RuleBuilder.Predicate("b"), RuleBuilder.Predicate("c")))
            .Compile(compiler)
            .CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EvaluatedNode skipped = decision.EvaluatedTree!.Children[1];
        Assert.True(skipped.NotEvaluated);
        Assert.Equal("IMPLIES", skipped.NodeDescription);
    }

    /// <summary>The operator's label and description explain material implication, and operands keep their order.</summary>
    [Fact]
    public void Describe_Implies_ExplainsStrongKleeneMaterialImplication_Test()
    {
        RuleDescription description = Compiler.Compile("a IMPLIES b").CompiledRule!.Describe();

        Assert.Equal("IMPLIES", description.Label);
        Assert.Contains("NOT", description.Description, StringComparison.Ordinal);
        Assert.Equal(["a", "b"], description.Operands.Select(o => o.Label));
    }

    /// <summary>Plain-text and Mermaid renderings show the arrow in the symbolic style and the word otherwise.</summary>
    [Fact]
    public void Print_ImpliesInSymbolicStyle_UsesTheArrow_Test()
    {
        CompiledRule<RuleTestContext> rule = Compiler.Compile("a IMPLIES b").CompiledRule!;

        Assert.Contains("→", PlainTextTreePrinter.Print(rule.Describe(), OperatorStyle.Symbolic), StringComparison.Ordinal);
        Assert.Contains("→", MermaidTreePrinter.Print(rule.Describe(), OperatorStyle.Symbolic), StringComparison.Ordinal);
        Assert.Contains("IMPLIES", rule.PrintMermaid(), StringComparison.Ordinal);
    }

    /// <summary>Changing the consequent of an IMPLIES is a single change at the consequent's path.</summary>
    [Fact]
    public void Compare_ImpliesWithChangedConsequent_ReportsTheConsequentPosition_Test()
    {
        CompiledRule<RuleTestContext> before = Compiler.Compile("a IMPLIES b").CompiledRule!;
        CompiledRule<RuleTestContext> after = Compiler.Compile("a IMPLIES c").CompiledRule!;

        RuleDiffResult diff = RuleDiff.Compare(before, after);

        RuleDiffEntry entry = Assert.Single(diff.Entries);
        Assert.Equal([1], entry.Path);
    }

    /// <summary>Changing between IMPLIES and another operator is reported as a change at the root.</summary>
    [Fact]
    public void Compare_ImpliesVersusXor_ReportsChangeAtTheRoot_Test()
    {
        CompiledRule<RuleTestContext> before = Compiler.Compile("a IMPLIES b").CompiledRule!;
        CompiledRule<RuleTestContext> after = Compiler.Compile("a XOR b").CompiledRule!;

        RuleDiffEntry entry = Assert.Single(RuleDiff.Compare(before, after).Entries);

        Assert.Empty(entry.Path);
        Assert.Equal("IMPLIES", entry.Before!.Label);
    }
}
