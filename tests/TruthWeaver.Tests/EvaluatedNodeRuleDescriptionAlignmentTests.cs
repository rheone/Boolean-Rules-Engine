namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <see cref="Evaluator{TContext}"/>'s <see cref="EvaluatedNode"/> tree and <c>CompiledRule.DescribeNode</c>'s
/// <see cref="RuleDescription"/> tree are two independently-invoked traversals that both consume the
/// same shared node-shape seam (<c>ExpressionShape.Of</c>), so they cannot structurally diverge in
/// operand order — the two trees stay positionally zippable by construction (see
/// <c>evaluated-node-rule-description-alignment</c> ticket 02), not merely by the two traversals
/// happening to agree.
/// </summary>
public sealed class EvaluatedNodeRuleDescriptionAlignmentTests
{
    [Theory]
    [InlineData("d AND a AND c")]
    [InlineData("d OR a OR c")]
    [InlineData("NOT d")]
    [InlineData("d XOR a")]
    [InlineData("d XNOR a")]
    [InlineData("d IMPLIES a")]
    [InlineData("d NAND a")]
    [InlineData("d NOR a")]
    [InlineData("NXOR(d, a, c)")]
    [InlineData("ANY(d, a, c)")]
    [InlineData("ALL(d, a, c)")]
    [InlineData("NONE(d, a, c)")]
    [InlineData("BETWEEN(1, 2, d, a, c)")]
    [InlineData("COALESCE(d, a, c)")]
    [InlineData("d ?? a ?? c")]
    [InlineData("If(d, a, c)")]
    [InlineData("d ? a : c")]
    [InlineData("IsTrue(d)")]
    [InlineData("IsFalse(d)")]
    [InlineData("IsUnknown(d AND a)")]
    [InlineData("IsKnown(d) OR IsKnown(a)")]
    [InlineData("Project(d, True)")]
    [InlineData("Project(d AND a, False) OR c")]
    [InlineData("ExactlyOne(d, a, c)")]
    [InlineData("AtLeast(2, d, a, c)")]
    [InlineData("(d AND a) OR (ExactlyOne(c, d, a) XOR (NOT c))")]
    public async Task Evaluated_tree_and_description_tree_agree_on_operand_order_at_every_level(string dsl)
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("c", false)
            .AddConstant("d", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);
        CompiledRule<RuleTestContext> rule = compiler.Compile(dsl).CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            new EvaluationOptions(Mode: EvaluationMode.Exhaustive),
            TestContext.Current.CancellationToken
        );

        RuleDescription description = rule.Describe();
        EvaluatedNode evaluated = decision.EvaluatedTree!;

        AssertAligned(description, evaluated);
    }

    private static void AssertAligned(RuleDescription description, EvaluatedNode evaluated)
    {
        Assert.Equal(description.Operands.Count, evaluated.Children.Count);
        for (int i = 0; i < description.Operands.Count; i++)
        {
            // A leaf term's RuleDescription.Label and EvaluatedNode.NodeDescription are both derived
            // from the same predicate name here, so if either traversal reordered its operands this
            // positional comparison would catch it at the first divergent leaf.
            AssertAligned(description.Operands[i], evaluated.Children[i]);
        }

        if (description.Operands.Count == 0 && evaluated.Children.Count == 0)
        {
            Assert.Equal(description.Label, evaluated.NodeDescription);
        }
    }
}
