namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

public sealed class TraceTests
{
    [Fact]
    public void An_evaluated_entry_carries_its_resolved_result()
    {
        TraceEntry entry = new("hasRole(role: \"Y\")", TruthValue.True, NotEvaluated: false);

        Assert.Equal(TruthValue.True, entry.Result);
        Assert.False(entry.NotEvaluated);
    }

    [Fact]
    public void A_skipped_entry_has_no_result()
    {
        TraceEntry entry = new("isSuspended", Result: null, NotEvaluated: true);

        Assert.Null(entry.Result);
        Assert.True(entry.NotEvaluated);
    }

    [Fact]
    public void Trace_preserves_entries_in_evaluation_order()
    {
        TraceEntry first = new("isManager", TruthValue.True, false);
        TraceEntry second = new("isSuspended", null, true);

        Trace trace = new([first, second]);

        Assert.Equal([first, second], trace.Entries);
    }
}
