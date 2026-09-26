namespace BooleanRulesEngine.Abstractions;

/// <summary>
/// The compile-time-known shape of a predicate: its registered name and its named-argument
/// declarations. <c>RuleCompiler</c> (in the <c>BooleanRulesEngine</c> package) validates every term
/// against its predicate's schema, so a missing or mistyped argument is a compile diagnostic rather
/// than a runtime failure inside <see cref="IPredicate{TContext}"/>'s evaluation method.
/// </summary>
/// <param name="Name">
/// The predicate's registered name. Term identity normalizes to this exact casing (CONTEXT.md).
/// </param>
/// <param name="Arguments">The argument declarations, or empty for a zero-argument predicate.</param>
public sealed record PredicateSchema(string Name, IReadOnlyList<PredicateArgumentSchema> Arguments)
{
    /// <summary>Creates a schema for a zero-argument predicate.</summary>
    /// <param name="name">The predicate's registered name.</param>
    /// <returns>A schema with no arguments.</returns>
    public static PredicateSchema NoArguments(string name)
    {
        return new(name, []);
    }
}
