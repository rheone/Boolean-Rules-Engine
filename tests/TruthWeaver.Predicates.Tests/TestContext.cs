namespace TruthWeaver.Predicates.Tests;

/// <summary>A minimal application context used to exercise selector-parameterized predicates.</summary>
/// <param name="Value">The string value a string-selector-based predicate reads.</param>
/// <param name="Values">The collection value a collection-selector-based predicate reads.</param>
internal sealed record TestContext(string? Value, IReadOnlyCollection<string>? Values = null);
