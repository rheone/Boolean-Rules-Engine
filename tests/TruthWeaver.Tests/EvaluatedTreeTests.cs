namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
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

    [Fact]
    public async Task Skipped_or_subtree_is_described_as_OR()
    {
        // "a AND (b OR c)" with a = false short-circuits before the (b OR c) OrExpression is
        // evaluated, so its skipped EvaluatedNode is labelled from the node's static shape alone.
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
        Assert.Equal("OR", skippedSubtree.NodeDescription);
    }

    [Fact]
    public async Task Skipped_and_subtree_is_described_as_AND()
    {
        // "a OR (b AND c)" with a = true short-circuits before the (b AND c) AndExpression is
        // evaluated, so its skipped EvaluatedNode is labelled from the node's static shape alone.
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("a OR (b AND c)")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        EvaluatedNode skippedSubtree = decision.EvaluatedTree!.Children[1];
        Assert.True(skippedSubtree.NotEvaluated);
        Assert.Equal("AND", skippedSubtree.NodeDescription);
    }

    [Fact]
    public async Task Skipped_not_subtree_is_described_as_NOT()
    {
        // "a AND (NOT b)" with a = false short-circuits before the (NOT b) NotExpression is
        // evaluated, so its skipped EvaluatedNode is labelled from the node's static shape alone.
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", false)
            .AddConstant("b", true)
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);

        Decision decision = await compiler
            .Compile("a AND (NOT b)")
            .CompiledRule!.EvaluateAsync(
                new RuleTestContext(),
                EmptyServiceProvider.Instance,
                cancellationToken: TestContext.Current.CancellationToken
            );

        EvaluatedNode skippedSubtree = decision.EvaluatedTree!.Children[1];
        Assert.True(skippedSubtree.NotEvaluated);
        Assert.Equal("NOT", skippedSubtree.NodeDescription);
    }

    [Fact]
    public async Task Skipped_xor_subtree_is_described_as_XOR()
    {
        // "a AND (b XOR c)" with a = false short-circuits before the (b XOR c) XorExpression is
        // evaluated, so its skipped EvaluatedNode is labelled from the node's static shape alone.
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", false)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build()
        );

        CompiledRule<RuleTestContext> rule = RuleBuilder
            .And(RuleBuilder.Predicate("a"), RuleBuilder.Xor(RuleBuilder.Predicate("b"), RuleBuilder.Predicate("c")))
            .Compile(compiler)
            .CompiledRule!;
        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EvaluatedNode skippedSubtree = decision.EvaluatedTree!.Children[1];
        Assert.True(skippedSubtree.NotEvaluated);
        Assert.Equal("XOR", skippedSubtree.NodeDescription);
    }

    [Fact]
    public async Task Skipped_xnor_subtree_is_described_as_XNOR()
    {
        // "a AND (b XNOR c)" with a = false short-circuits before the (b XNOR c) XnorExpression is
        // evaluated, so its skipped EvaluatedNode is labelled from the node's static shape alone.
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", false)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .Build()
        );

        CompiledRule<RuleTestContext> rule = RuleBuilder
            .And(RuleBuilder.Predicate("a"), RuleBuilder.Xnor(RuleBuilder.Predicate("b"), RuleBuilder.Predicate("c")))
            .Compile(compiler)
            .CompiledRule!;
        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EvaluatedNode skippedSubtree = decision.EvaluatedTree!.Children[1];
        Assert.True(skippedSubtree.NotEvaluated);
        Assert.Equal("XNOR", skippedSubtree.NodeDescription);
    }

    [Fact]
    public async Task Skipped_exactlyone_subtree_is_described_as_ExactlyOne()
    {
        // "a AND ExactlyOne(b, c, d)" with a = false short-circuits before the ExactlyOneExpression
        // is evaluated, so its skipped EvaluatedNode is labelled from the node's static shape alone.
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", false)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .AddConstant("d", true)
                .Build()
        );

        CompiledRule<RuleTestContext> rule = RuleBuilder
            .And(
                RuleBuilder.Predicate("a"),
                RuleBuilder.ExactlyOne(RuleBuilder.Predicate("b"), RuleBuilder.Predicate("c"), RuleBuilder.Predicate("d"))
            )
            .Compile(compiler)
            .CompiledRule!;
        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EvaluatedNode skippedSubtree = decision.EvaluatedTree!.Children[1];
        Assert.True(skippedSubtree.NotEvaluated);
        Assert.Equal("ExactlyOne", skippedSubtree.NodeDescription);
    }

    [Fact]
    public async Task Skipped_threshold_subtree_is_described_using_the_default_OpName_K_format()
    {
        // "a AND AtLeast(2, b, c, d)" with a = false short-circuits before the ThresholdExpression is
        // evaluated, so its skipped EvaluatedNode falls through to the default arm's
        // "OpName(K)" formatting rather than one of the named arms.
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddConstant("a", false)
                .AddConstant("b", true)
                .AddConstant("c", true)
                .AddConstant("d", true)
                .Build()
        );

        CompiledRule<RuleTestContext> rule = RuleBuilder
            .And(
                RuleBuilder.Predicate("a"),
                RuleBuilder.AtLeast(2, RuleBuilder.Predicate("b"), RuleBuilder.Predicate("c"), RuleBuilder.Predicate("d"))
            )
            .Compile(compiler)
            .CompiledRule!;
        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        EvaluatedNode skippedSubtree = decision.EvaluatedTree!.Children[1];
        Assert.True(skippedSubtree.NotEvaluated);
        Assert.Equal("AtLeast(2)", skippedSubtree.NodeDescription);
    }
}
