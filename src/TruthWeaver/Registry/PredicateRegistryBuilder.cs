namespace TruthWeaver.Registry;

using TruthWeaver.Abstractions;

/// <summary>
/// Builds a <see cref="PredicateRegistry{TContext}"/> via explicit registration — either a
/// class-based <see cref="IPredicate{TContext}"/> (resolved per-evaluation from
/// <see cref="IServiceProvider"/>) or a stateless lambda. No attribute or assembly scanning
/// (ADR-0004).
/// </summary>
/// <typeparam name="TContext">The application context type predicates read from.</typeparam>
public sealed class PredicateRegistryBuilder<TContext>
{
    private readonly Dictionary<string, PredicateDescriptor<TContext>> descriptorsByName = [];

    /// <summary>Registers a class-based predicate, resolved from the per-evaluation <see cref="IServiceProvider"/>.</summary>
    /// <typeparam name="TPredicate">The predicate implementation type.</typeparam>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException">A predicate with the same name (case-insensitive) is already registered.</exception>
    public PredicateRegistryBuilder<TContext> Add<TPredicate>()
        where TPredicate : IPredicate<TContext>
    {
        PredicateSchema schema = TPredicate.Schema;
        this.AddDescriptor(schema.Name, new PredicateDescriptor<TContext>(schema, typeof(TPredicate)));
        return this;
    }

    /// <summary>Registers a stateless lambda predicate.</summary>
    /// <param name="schema">The predicate's schema.</param>
    /// <param name="evaluate">The stateless evaluation function.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException">A predicate with the same name (case-insensitive) is already registered.</exception>
    public PredicateRegistryBuilder<TContext> Add(
        PredicateSchema schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate
    )
    {
        this.AddDescriptor(schema.Name, new PredicateDescriptor<TContext>(schema, evaluate));
        return this;
    }

    /// <summary>Builds the immutable registry.</summary>
    /// <returns>The built registry.</returns>
    public PredicateRegistry<TContext> Build()
    {
        return new(this.descriptorsByName);
    }

    private void AddDescriptor(string name, PredicateDescriptor<TContext> descriptor)
    {
        string key = name.ToUpperInvariant();
        if (!this.descriptorsByName.TryAdd(key, descriptor))
        {
            throw new ArgumentException($"A predicate named '{name}' (case-insensitive) is already registered.", nameof(name));
        }
    }
}
