namespace TruthWeaver.Testing.Tests;

public sealed class SimulatedPredicateFaultExceptionTests
{
    [Fact]
    public void ParameterlessConstructor_ProducesDefaultState()
    {
        SimulatedPredicateFaultException exception = new();

        Assert.False(string.IsNullOrEmpty(exception.Message));
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void MessageConstructor_SetsMessage()
    {
        SimulatedPredicateFaultException exception = new("predicate fault");

        Assert.Equal("predicate fault", exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void MessageAndInnerExceptionConstructor_SetsBoth()
    {
        InvalidOperationException inner = new("boom");

        SimulatedPredicateFaultException exception = new("predicate fault", inner);

        Assert.Equal("predicate fault", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }
}
