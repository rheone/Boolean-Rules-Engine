namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

public sealed class TruthValueAndDecisionTests
{
    [Fact]
    public void Decision_is_satisfied_only_when_result_is_true()
    {
        Decision trueDecision = new(TruthValue.True, []);
        Decision falseDecision = new(TruthValue.False, []);
        Decision unknownDecision = new(TruthValue.Unknown, []);

        Assert.True(trueDecision.IsSatisfied);
        Assert.False(falseDecision.IsSatisfied);
        Assert.False(unknownDecision.IsSatisfied);
    }

    [Fact]
    public void Decision_carries_faults_in_recorded_order()
    {
        Fault first = new(new TermIdentity("a", []), new InvalidOperationException("first"));
        Fault second = new(new TermIdentity("b", []), new InvalidOperationException("second"));

        Decision decision = new(TruthValue.Unknown, [first, second]);

        Assert.Equal([first, second], decision.Faults);
    }

    [Fact]
    public void Decision_trace_and_evaluated_tree_default_to_null()
    {
        Decision decision = new(TruthValue.True, []);

        Assert.Null(decision.Trace);
        Assert.Null(decision.EvaluatedTree);
    }

    [Fact]
    public void Decision_outcome_defaults_to_null_when_no_collapse_was_declared()
    {
        Decision decision = new(TruthValue.Unknown, []);

        Assert.Null(decision.Outcome);
    }

    [Theory]
    [InlineData(TruthValue.True, CollapsePolicy.UnknownAsFalse, CollapseOutcome.True)]
    [InlineData(TruthValue.True, CollapsePolicy.UnknownAsTrue, CollapseOutcome.True)]
    [InlineData(TruthValue.True, CollapsePolicy.UnknownIsError, CollapseOutcome.True)]
    [InlineData(TruthValue.False, CollapsePolicy.UnknownAsFalse, CollapseOutcome.False)]
    [InlineData(TruthValue.False, CollapsePolicy.UnknownAsTrue, CollapseOutcome.False)]
    [InlineData(TruthValue.False, CollapsePolicy.UnknownIsError, CollapseOutcome.False)]
    [InlineData(TruthValue.Unknown, CollapsePolicy.UnknownAsFalse, CollapseOutcome.False)]
    [InlineData(TruthValue.Unknown, CollapsePolicy.UnknownAsTrue, CollapseOutcome.True)]
    [InlineData(TruthValue.Unknown, CollapsePolicy.UnknownIsError, CollapseOutcome.RejectedUnresolved)]
    public void Decision_collapse_resolves_only_unknown_by_policy(
        TruthValue result,
        CollapsePolicy policy,
        CollapseOutcome expected
    )
    {
        Decision decision = new(result, []);

        Assert.Equal(expected, decision.Collapse(policy));
    }

    [Fact]
    public void Decision_collapse_with_an_undefined_policy_throws_argument_out_of_range()
    {
        Decision decision = new(TruthValue.Unknown, []);

        Assert.Throws<ArgumentOutOfRangeException>(() => decision.Collapse((CollapsePolicy)99));
    }

    [Fact]
    public void Decision_collapse_does_not_change_the_decision_or_its_fail_closed_satisfaction()
    {
        Fault fault = new(new TermIdentity("a", []), new InvalidOperationException("boom"));
        Decision decision = new(TruthValue.Unknown, [fault]);

        CollapseOutcome outcome = decision.Collapse(CollapsePolicy.UnknownAsTrue);

        Assert.Equal(CollapseOutcome.True, outcome);
        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.False(decision.IsSatisfied);
        Assert.Equal([fault], decision.Faults);
        Assert.Null(decision.Outcome);
    }

    [Fact]
    public void Decision_collapse_rejection_is_not_recorded_as_a_fault()
    {
        Decision decision = new(TruthValue.Unknown, []);

        CollapseOutcome outcome = decision.Collapse(CollapsePolicy.UnknownIsError);

        Assert.Equal(CollapseOutcome.RejectedUnresolved, outcome);
        Assert.Empty(decision.Faults);
    }
}
