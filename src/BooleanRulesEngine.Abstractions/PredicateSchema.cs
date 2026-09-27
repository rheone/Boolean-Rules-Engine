namespace BooleanRulesEngine.Abstractions;

/// <summary>
/// The compile-time-known shape of a predicate: its registered name, a human-readable description,
/// and its named-argument declarations. <c>RuleCompiler</c> (in the <c>BooleanRulesEngine</c>
/// package) validates every term against its predicate's schema, so a missing or mistyped argument
/// is a compile diagnostic rather than a runtime failure inside <see cref="IPredicate{TContext}"/>'s
/// evaluation method.
/// </summary>
/// <param name="Name">
/// The predicate's registered name. Term identity normalizes to this exact casing (CONTEXT.md).
/// </param>
/// <param name="Description">
/// A human-readable, read-only description of what this predicate answers (e.g. "Does the current
/// user hold the given role?"). Required so a rule-authoring UI or generated documentation always
/// has something to show for every registered predicate, never an empty string.
/// </param>
/// <param name="Arguments">The argument declarations, or empty for a zero-argument predicate.</param>
public sealed record PredicateSchema(string Name, string Description, IReadOnlyList<PredicateArgumentSchema> Arguments)
{
    /// <summary>Creates a schema for a zero-argument predicate.</summary>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="description">A human-readable description of what this predicate answers.</param>
    /// <returns>A schema with no arguments.</returns>
    public static PredicateSchema NoArguments(string name, string description)
    {
        return new(name, description, []);
    }
}
