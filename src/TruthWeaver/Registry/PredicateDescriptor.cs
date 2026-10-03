namespace TruthWeaver.Registry;

using TruthWeaver.Abstractions;

/// <summary>
/// A registered predicate's compile-time-known shape plus how to invoke it at evaluation time —
/// either a stateless lambda, or an implementation type resolved fresh from the per-evaluation
/// <see cref="IServiceProvider"/> (ADR-0002).
/// </summary>
/// <typeparam name="TContext">The application context type this predicate reads from.</typeparam>
internal sealed class PredicateDescriptor<TContext>
{
    /// <summary>Initializes a new instance of the <see cref="PredicateDescriptor{TContext}"/> class for a stateless lambda registration.</summary>
    /// <param name="schema">The predicate's schema.</param>
    /// <param name="evaluate">The stateless evaluation function.</param>
    public PredicateDescriptor(
        PredicateSchema schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate
    )
    {
        this.Schema = schema;
        this.Evaluate = evaluate;
    }

    /// <summary>Initializes a new instance of the <see cref="PredicateDescriptor{TContext}"/> class for a class-based registration.</summary>
    /// <param name="schema">The predicate's schema.</param>
    /// <param name="implementationType">The concrete <see cref="IPredicate{TContext}"/> type, resolved per-evaluation.</param>
    public PredicateDescriptor(PredicateSchema schema, Type implementationType)
    {
        this.Schema = schema;
        this.ImplementationType = implementationType;
    }

    /// <summary>Gets the predicate's schema, including its registered (canonical-cased) name.</summary>
    public PredicateSchema Schema { get; }

    /// <summary>Gets the stateless evaluation lambda, if this descriptor was registered that way.</summary>
    public Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>>? Evaluate { get; }

    /// <summary>Gets the class-based implementation type, if this descriptor was registered that way.</summary>
    public Type? ImplementationType { get; }
}
