namespace TruthWeaver.Abstractions;

/// <summary>
/// A registered, reusable three-valued (Strong Kleene) condition over an application-supplied context — the function a
/// rule's terms bind arguments to and call (CONTEXT.md). Implementations should be stateless; any
/// per-call dependency (a <c>DbContext</c>, a scoped <c>HttpClient</c>) is resolved by the engine
/// from the <see cref="IServiceProvider"/> supplied to each evaluation, never captured once at
/// registration (ADR-0002).
/// </summary>
/// <typeparam name="TContext">The application-owned context type this predicate reads from.</typeparam>
public interface IPredicate<in TContext>
{
    // SonarAnalyzer's S2743 predates C# 11 static abstract interface members and misreads this as a
    // single static field shared across closed generic types; each implementing type in fact declares
    // its own Schema, which is the entire point of a static abstract interface member.
#pragma warning disable S2743
    /// <summary>Gets the predicate's registered name and argument schema, validated at compile time.</summary>
    public static abstract PredicateSchema Schema { get; }
#pragma warning restore S2743

    /// <summary>
    /// Evaluates this predicate for one term. Return <see cref="TruthValue.Unknown"/> when the
    /// answer is legitimately indeterminate (for example the data is not available); that is a normal
    /// result and records no <see cref="Fault"/>. Signal a genuine failure (a timeout, a connection
    /// failure) by simply throwing — the evaluator absorbs the exception as a <see cref="Fault"/>
    /// and treats the term as <see cref="TruthValue.Unknown"/> (ADR-0001, ADR-0005); no
    /// try/catch-and-wrap boilerplate is expected of the implementation.
    /// </summary>
    /// <param name="context">The application-supplied evaluation context.</param>
    /// <param name="args">This term's arguments, validated against <see cref="Schema"/> at compile time.</param>
    /// <param name="cancellationToken">A token observed for cooperative cancellation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TruthValue}"/> resolving to this term's Kleene answer for the given context.
    /// </returns>
    public ValueTask<TruthValue> EvaluateAsync(TContext context, PredicateArguments args, CancellationToken cancellationToken);
}
