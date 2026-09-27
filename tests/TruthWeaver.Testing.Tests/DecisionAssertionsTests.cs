namespace TruthWeaver.Testing.Tests;

using TruthWeaver.Abstractions;

public sealed class DecisionAssertionsTests
{
    [Fact]
    public void BeSatisfied_TrueResult_DoesNotThrow()
    {
        Decision decision = new(TruthValue.True, []);

        decision.Should().BeSatisfied();
    }

    [Fact]
    public void BeSatisfied_FalseResult_Throws()
    {
        Decision decision = new(TruthValue.False, []);

        Assert.Throws<DecisionAssertionException>(() => decision.Should().BeSatisfied());
    }

    [Fact]
    public void BeSatisfied_UnknownResult_Throws()
    {
        // Decision.IsSatisfied fails closed on Unknown (ADR-0001), and the assertion mirrors that.
        Decision decision = new(TruthValue.Unknown, []);

        Assert.Throws<DecisionAssertionException>(() => decision.Should().BeSatisfied());
    }

    [Fact]
    public void NotBeSatisfied_FalseResult_DoesNotThrow()
    {
        Decision decision = new(TruthValue.False, []);

        decision.Should().NotBeSatisfied();
    }

    [Fact]
    public void NotBeSatisfied_TrueResult_Throws()
    {
        Decision decision = new(TruthValue.True, []);

        Assert.Throws<DecisionAssertionException>(() => decision.Should().NotBeSatisfied());
    }

    [Fact]
    public void HaveResult_MatchingResult_DoesNotThrow()
    {
        Decision decision = new(TruthValue.Unknown, []);

        decision.Should().HaveResult(TruthValue.Unknown);
    }

    [Fact]
    public void HaveResult_MismatchedResult_Throws()
    {
        Decision decision = new(TruthValue.Unknown, []);

        DecisionAssertionException exception = Assert.Throws<DecisionAssertionException>(() =>
            decision.Should().HaveResult(TruthValue.True)
        );
        Assert.Contains("True", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Unknown", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void HaveNoFaults_EmptyFaultList_DoesNotThrow()
    {
        Decision decision = new(TruthValue.True, []);

        decision.Should().HaveNoFaults();
    }

    [Fact]
    public void HaveNoFaults_WithFaults_Throws()
    {
        Decision decision = new(TruthValue.Unknown, [SampleFault()]);

        Assert.Throws<DecisionAssertionException>(() => decision.Should().HaveNoFaults());
    }

    [Fact]
    public void HaveFault_WithFaults_DoesNotThrow()
    {
        Decision decision = new(TruthValue.Unknown, [SampleFault()]);

        decision.Should().HaveFault();
    }

    [Fact]
    public void HaveFault_EmptyFaultList_Throws()
    {
        Decision decision = new(TruthValue.True, []);

        Assert.Throws<DecisionAssertionException>(() => decision.Should().HaveFault());
    }

    [Fact]
    public void HaveFaultCount_MatchingCount_DoesNotThrow()
    {
        Decision decision = new(TruthValue.Unknown, [SampleFault(), SampleFault()]);

        decision.Should().HaveFaultCount(2);
    }

    [Fact]
    public void HaveFaultCount_MismatchedCount_Throws()
    {
        Decision decision = new(TruthValue.Unknown, [SampleFault()]);

        Assert.Throws<DecisionAssertionException>(() => decision.Should().HaveFaultCount(2));
    }

    [Fact]
    public void HaveFaultForTerm_MatchingPredicateName_DoesNotThrow()
    {
        Decision decision = new(TruthValue.Unknown, [SampleFault("hasRole")]);

        decision.Should().HaveFaultForTerm("hasRole");
    }

    [Fact]
    public void HaveFaultForTerm_NoMatchingPredicateName_Throws()
    {
        Decision decision = new(TruthValue.Unknown, [SampleFault("hasRole")]);

        Assert.Throws<DecisionAssertionException>(() => decision.Should().HaveFaultForTerm("isManager"));
    }

    [Fact]
    public void Assertions_ChainFluently()
    {
        Decision decision = new(TruthValue.True, []);

        decision.Should().HaveResult(TruthValue.True).BeSatisfied().HaveNoFaults();
    }

    private static Fault SampleFault(string predicateName = "hasRole")
    {
        return new Fault(new TermIdentity(predicateName, []), new InvalidOperationException("simulated"));
    }
}
