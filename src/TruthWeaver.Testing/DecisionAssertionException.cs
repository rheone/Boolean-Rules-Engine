namespace TruthWeaver.Testing;

/// <summary>
/// Thrown by a <see cref="DecisionAssertions"/> method when the asserted condition does not hold.
/// Deliberately framework-agnostic (not an xUnit/FluentAssertions exception type): this package takes
/// no dependency on any particular test framework's assertion library, so any test runner that treats
/// an uncaught exception as a test failure — xUnit included — reports it correctly.
/// </summary>
public sealed class DecisionAssertionException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="DecisionAssertionException"/> class.</summary>
    public DecisionAssertionException() { }

    /// <summary>Initializes a new instance of the <see cref="DecisionAssertionException"/> class.</summary>
    /// <param name="message">The assertion failure message.</param>
    public DecisionAssertionException(string message)
        : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="DecisionAssertionException"/> class.</summary>
    /// <param name="message">The assertion failure message.</param>
    /// <param name="innerException">The exception that caused this assertion to fail.</param>
    public DecisionAssertionException(string message, Exception innerException)
        : base(message, innerException) { }
}
