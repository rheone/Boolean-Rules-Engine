namespace TruthWeaver.Testing;

/// <summary>
/// The exception a <see cref="FakePredicates"/>-created predicate throws to simulate a fault
/// (ADR-0001) when it is configured with an <c>Unknown</c> result. The evaluator absorbs this
/// exception exactly as it would absorb a real predicate's failure, and treats the faulted term as
/// <c>TruthValue.Unknown</c> for the remainder of that evaluation.
/// </summary>
public sealed class SimulatedPredicateFaultException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="SimulatedPredicateFaultException"/> class.</summary>
    public SimulatedPredicateFaultException() { }

    /// <summary>Initializes a new instance of the <see cref="SimulatedPredicateFaultException"/> class.</summary>
    /// <param name="message">The exception message.</param>
    public SimulatedPredicateFaultException(string message)
        : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="SimulatedPredicateFaultException"/> class.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The exception that caused this fault.</param>
    public SimulatedPredicateFaultException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>Creates the exception a fake predicate named <paramref name="predicateName"/> throws to simulate a fault.</summary>
    /// <param name="predicateName">The fake predicate's registered name.</param>
    /// <returns>A new <see cref="SimulatedPredicateFaultException"/> with a descriptive message.</returns>
    public static SimulatedPredicateFaultException ForPredicate(string predicateName)
    {
        return new($"Fake predicate '{predicateName}' was configured to simulate a fault (an Unknown result).");
    }
}
