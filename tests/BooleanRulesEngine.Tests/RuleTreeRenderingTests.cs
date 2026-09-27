namespace BooleanRulesEngine.Tests;

using BooleanRulesEngine.Abstractions;
using BooleanRulesEngine.Compilation;
using BooleanRulesEngine.Evaluation;
using BooleanRulesEngine.Registry;
using BooleanRulesEngine.Tests.TestSupport;

/// <summary>
/// <see cref="CompiledRule{TContext}.PrintMermaid()"/>/<see cref="CompiledRule{TContext}.PrintPlainText()"/>
/// — structure-only and evaluation-colored tree rendering, for delivery to a diagram UI or a log.
/// </summary>
public sealed class RuleTreeRenderingTests
{
    [Fact]
    public void Structural_mermaid_output_has_no_evaluation_coloring()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "a AND b",
            registry => registry.AddConstant("a", true).AddConstant("b", true)
        );

        string mermaid = rule.PrintMermaid();

        Assert.StartsWith("flowchart TD", mermaid);
        Assert.Contains("AND", mermaid);
        Assert.Contains("-->", mermaid);
        Assert.DoesNotContain("classDef", mermaid);
    }

    [Fact]
    public async Task Evaluated_mermaid_output_colors_true_false_and_skipped_nodes()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "a AND b",
            registry => registry.AddConstant("a", false).AddConstant("b", true)
        );

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        string mermaid = rule.PrintMermaid(decision);

        Assert.Contains("classDef brTrue", mermaid);
        Assert.Contains("classDef brFalse", mermaid);
        Assert.Contains("classDef brSkipped", mermaid);
        Assert.Contains("brFalse", mermaid);
        Assert.Contains("brSkipped", mermaid);
    }

    [Fact]
    public void PrintMermaid_rejects_a_decision_with_no_evaluated_tree()
    {
        CompiledRule<RuleTestContext> rule = Compile("a", registry => registry.AddConstant("a", true));
        Decision decisionWithoutTree = new(TruthValue.True, []);

        Assert.Throws<ArgumentException>(() => rule.PrintMermaid(decisionWithoutTree));
    }

    [Fact]
    public void Structural_plain_text_output_has_no_evaluation_suffixes()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "a AND b",
            registry => registry.AddConstant("a", true).AddConstant("b", true)
        );

        string text = rule.PrintPlainText();

        Assert.Contains("AND", text);
        Assert.DoesNotContain("[true]", text);
        Assert.DoesNotContain("[skipped]", text);
    }

    [Fact]
    public async Task Evaluated_plain_text_output_marks_the_short_circuited_operand_as_skipped()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "a AND b",
            registry => registry.AddConstant("a", false).AddConstant("b", true)
        );

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        string text = rule.PrintPlainText(decision);

        Assert.Contains("[false]", text);
        Assert.Contains("[skipped]", text);
        Assert.DoesNotContain("[true]", text);
    }

    private static CompiledRule<RuleTestContext> Compile(
        string dsl,
        Func<PredicateRegistryBuilder<RuleTestContext>, PredicateRegistryBuilder<RuleTestContext>> configure
    )
    {
        PredicateRegistry<RuleTestContext> registry = configure(PredicateRegistry<RuleTestContext>.CreateBuilder()).Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);
        return compiler.Compile(dsl).CompiledRule!;
    }
}
