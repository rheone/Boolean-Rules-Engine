namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <see cref="Decision.EvaluatedTree"/>: a structural mirror of the compiled tree, annotated per node
/// by what happened during evaluation, in lockstep with the flat <see cref="Trace"/> the same
/// evaluation produces.
/// </summary>
public sealed class EvaluatedTreeTests
{
    [Fact]
    public async Task Every_node_in_a_fully_evaluated_tree_has_a_result()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("a AND b")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        Assert.NotNull(decision.EvaluatedTree);
        EvaluatedNode root = decision.EvaluatedTree;
        Assert.Equal(TruthValue.True, root.Result);
        Assert.False(root.NotEvaluated);
        Assert.Equal(2, root.Children.Count);
        Assert.All(root.Children, child => Assert.Equal(TruthValue.True, child.Result));
    }

    [Fact]
    public async Task And_short_circuit_marks_the_skipped_operand_not_evaluated_with_no_children()
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", false)
            .AddConstant("b", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("a AND b")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        EvaluatedNode root = decision.EvaluatedTree!;
        Assert.Equal(TruthValue.False, root.Result);
        EvaluatedNode skippedOperand = root.Children[1];
        Assert.True(skippedOperand.NotEvaluated);
        Assert.Null(skippedOperand.Result);
        Assert.Empty(skippedOperand.Children);
    }

    [Fact]
    public async Task Skipping_a_whole_subtree_operand_leaves_it_with_no_children_despite_its_own_static_shape()
    {
        // "a AND (b OR c)" with a = false: the (b OR c) OrExpression is itself the second AND operand,
        // so the whole subtree is skipped as one unit — nothing about b or c is recorded.
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", false)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("a AND (b OR c)")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        EvaluatedNode skippedSubtree = decision.EvaluatedTree!.Children[1];
        Assert.True(skippedSubtree.NotEvaluated);
        Assert.Empty(skippedSubtree.Children);
    }
}
