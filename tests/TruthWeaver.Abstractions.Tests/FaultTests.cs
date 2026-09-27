namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

public sealed class FaultTests
{
    [Fact]
    public void Fault_records_the_faulting_term_and_its_exception()
    {
        TermIdentity term = new("hasRole", []);
        InvalidOperationException exception = new("boom");

        Fault fault = new(term, exception);

        Assert.Same(term, fault.Term);
        Assert.Same(exception, fault.Exception);
    }
}
