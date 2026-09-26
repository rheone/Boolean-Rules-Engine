namespace BooleanRulesEngine.Tests.TestSupport;

/// <summary>A scoped dependency abstraction used to prove class-based predicates resolve per-evaluation (ticket 12).</summary>
public interface IScopedFlag
{
    /// <summary>Gets a value indicating whether this scope's flag is set.</summary>
    public bool Value { get; }
}
