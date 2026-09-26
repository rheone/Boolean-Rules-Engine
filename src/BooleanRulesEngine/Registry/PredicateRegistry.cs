namespace BooleanRulesEngine.Registry;

/// <summary>
/// Where predicate implementations are registered under a name, with their argument schema
/// (CONTEXT.md). Immutable once built; registration is exclusively through
/// <see cref="PredicateRegistryBuilder{TContext}"/> — there is no attribute or assembly-scanning
/// discovery path (ADR-0004).
/// </summary>
/// <typeparam name="TContext">The application context type predicates in this registry read from.</typeparam>
public sealed class PredicateRegistry<TContext>
{
    private readonly IReadOnlyDictionary<string, PredicateDescriptor<TContext>> descriptorsByName;

    internal PredicateRegistry(IReadOnlyDictionary<string, PredicateDescriptor<TContext>> descriptorsByName)
    {
        this.descriptorsByName = descriptorsByName;
    }

    /// <summary>Creates a builder for constructing a new registry.</summary>
    /// <returns>A new, empty builder.</returns>
    public static PredicateRegistryBuilder<TContext> CreateBuilder()
    {
        return new();
    }

    /// <summary>Looks up a predicate by name, case-insensitively.</summary>
    /// <param name="name">The predicate name as written in rule text.</param>
    /// <param name="descriptor">The matching descriptor, if found.</param>
    /// <returns><see langword="true"/> if a predicate with this name (case-insensitive) is registered.</returns>
    internal bool TryGet(string name, out PredicateDescriptor<TContext>? descriptor)
    {
        return this.descriptorsByName.TryGetValue(name.ToUpperInvariant(), out descriptor);
    }
}
