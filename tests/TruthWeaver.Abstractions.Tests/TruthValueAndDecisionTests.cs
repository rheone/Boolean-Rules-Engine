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
}
