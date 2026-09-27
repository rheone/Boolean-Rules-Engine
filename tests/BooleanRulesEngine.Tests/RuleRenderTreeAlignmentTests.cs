namespace BooleanRulesEngine.Tests;

using BooleanRulesEngine.Abstractions;
using BooleanRulesEngine.Evaluation;
using BooleanRulesEngine.Printing;

/// <summary>
/// <see cref="RuleRenderTree.Build(RuleDescription, EvaluatedNode, OperatorStyle)"/> zips a <see cref="RuleDescription"/>
/// and an <see cref="EvaluatedNode"/> positionally, on the assumption that both trees were built from the
/// same operand order. This guards that invariant: a deliberately mismatched pair of trees should fail
/// loudly (debug-only) instead of silently mislabeling evaluation state.
/// </summary>
public sealed class RuleRenderTreeAlignmentTests
{
    [Fact]
    public void Build_throws_when_evaluated_children_count_does_not_match_description_operand_count()
    {
        RuleDescription description = new(
            "AND",
            "Both operands must be true.",
            [new RuleDescription("a", "a", []), new RuleDescription("b", "b", [])]
        );
        EvaluatedNode evaluated = new(
            "AND",
            TruthValue.True,
            NotEvaluated: false,
            [new EvaluatedNode("a", TruthValue.True, false, [])]
        );

        Assert.Throws<InvalidOperationException>(() => RuleRenderTree.Build(description, evaluated));
    }

    [Fact]
    public void Build_does_not_throw_when_evaluated_children_count_matches_description_operand_count()
    {
        RuleDescription description = new(
            "AND",
            "Both operands must be true.",
            [new RuleDescription("a", "a", []), new RuleDescription("b", "b", [])]
        );
        EvaluatedNode evaluated = new(
            "AND",
            TruthValue.True,
            NotEvaluated: false,
            [new EvaluatedNode("a", TruthValue.True, false, []), new EvaluatedNode("b", TruthValue.True, false, [])]
        );

        RenderNode render = RuleRenderTree.Build(description, evaluated);

        Assert.Equal(RenderState.True, render.State);
    }
}
