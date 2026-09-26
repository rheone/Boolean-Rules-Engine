namespace BooleanRulesEngine.Abstractions;

/// <summary>
/// A literal, ordered record of every node an evaluation visited or explicitly skipped.
/// </summary>
/// <param name="Entries">The trace entries, in evaluation (left-to-right, depth-first) order.</param>
public sealed record Trace(IReadOnlyList<TraceEntry> Entries);
