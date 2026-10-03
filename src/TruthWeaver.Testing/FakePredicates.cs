namespace TruthWeaver.Testing;

using TruthWeaver.Abstractions;

/// <summary>
/// Fake/stub predicate factories for tests: a fixed answer, a Kleene (<see cref="TruthValue"/>)
/// answer, a simulated fault, or a scripted sequence of answers across successive calls — without
/// writing a hand-written <see cref="IPredicate{TContext}"/> class per test. Mirrors the shape of
/// <c>TruthWeaver.Predicates</c>'s ready-made predicate factories: each method returns the
/// schema plus a stateless evaluation delegate, ready for
/// <c>PredicateRegistryBuilder&lt;TContext&gt;.Add(schema, evaluate)</c>.
/// </summary>
public static class FakePredicates
{
    /// <summary>Creates a predicate that always returns a fixed definite result: <see cref="TruthValue.True"/> or <see cref="TruthValue.False"/>.</summary>
    /// <typeparam name="TContext">The application context type (ignored by the fake).</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="result">The fixed result every call returns.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="description">A human-readable description of this fake predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Returning<TContext>(
        string name,
        bool result,
        string label = "Fake",
        string description = "A fake predicate that always returns a fixed result, registered for a test."
    )
    {
        PredicateSchema schema = PredicateSchema.NoArguments(name, label, description);
        return (schema, (_, _, _) => ValueTask.FromResult(result ? TruthValue.True : TruthValue.False));
    }

    /// <summary>
    /// Creates a predicate that always answers with a fixed three-valued <see cref="TruthValue"/>.
    /// <see cref="TruthValue.Unknown"/> is returned directly, exactly as a real predicate would answer
    /// "indeterminate": it records no <see cref="Fault"/>. To simulate a real failure (an exception,
    /// which the evaluator records as a <see cref="Fault"/>), use
    /// <see cref="Faulting{TContext}(string, Exception, string, string)"/> instead.
    /// </summary>
    /// <typeparam name="TContext">The application context type (ignored by the fake).</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="result">The fixed three-valued result every call answers with.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="description">A human-readable description of this fake predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Returning<TContext>(
        string name,
        TruthValue result,
        string label = "Fake",
        string description = "A fake predicate that always answers with a fixed Kleene result, registered for a test."
    )
    {
        PredicateSchema schema = PredicateSchema.NoArguments(name, label, description);
        return (schema, (_, _, _) => ValueTask.FromResult(result));
    }

    /// <summary>
    /// Creates a predicate that always throws <paramref name="exception"/>, so the evaluator absorbs
    /// it as a <see cref="Fault"/> and treats the term as <see cref="TruthValue.Unknown"/> (ADR-0001).
    /// </summary>
    /// <typeparam name="TContext">The application context type (ignored by the fake).</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="exception">The exception every call throws.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="description">A human-readable description of this fake predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Faulting<TContext>(
        string name,
        Exception exception,
        string label = "Fake",
        string description =
            "A fake predicate that always throws, simulating a fault (recorded, Unknown), registered for a test."
    )
    {
        ArgumentNullException.ThrowIfNull(exception);
        PredicateSchema schema = PredicateSchema.NoArguments(name, label, description);
        return (schema, (_, _, _) => throw exception);
    }

    /// <summary>
    /// Creates a predicate that answers with successive entries from <paramref name="script"/>, one
    /// per call, in order — useful for testing memoization boundaries or a sequence of evaluations
    /// against the same rule with a predicate whose answer changes over time (e.g. a simulated flap).
    /// A <see cref="TruthValue.Unknown"/> entry answers Unknown on that call without a fault, the same
    /// as <see cref="Returning{TContext}(string, TruthValue, string, string)"/>.
    /// Calling the predicate more times than <paramref name="script"/> has entries throws
    /// <see cref="InvalidOperationException"/>, so an under-scripted test fails loudly rather than
    /// silently repeating or wrapping around.
    /// </summary>
    /// <typeparam name="TContext">The application context type (ignored by the fake).</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="script">The ordered sequence of answers, one consumed per call.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="description">A human-readable description of this fake predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    /// <exception cref="ArgumentException"><paramref name="script"/> is empty.</exception>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) Scripted<TContext>(
        string name,
        IReadOnlyList<TruthValue> script,
        string label = "Fake",
        string description =
            "A fake predicate that answers with successive scripted results across calls, registered for a test."
    )
    {
        if (script.Count == 0)
        {
            throw new ArgumentException("A scripted fake predicate needs at least one scripted answer.", nameof(script));
        }

        PredicateSchema schema = PredicateSchema.NoArguments(name, label, description);
        int callCount = 0;
        return (
            schema,
            (_, _, _) =>
            {
                int index = Interlocked.Increment(ref callCount) - 1;
                if (index >= script.Count)
                {
                    throw new InvalidOperationException(
                        $"Scripted fake predicate '{name}' was called {index + 1} times, "
                            + $"but only {script.Count} answer(s) were scripted."
                    );
                }

                return ValueTask.FromResult(script[index]);
            }
        );
    }
}
