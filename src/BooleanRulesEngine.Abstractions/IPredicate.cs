namespace BooleanRulesEngine.Abstractions;

/// <summary>
/// A registered, reusable boolean condition over an application-supplied context — the function a
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
    /// Evaluates this predicate for one term. Signal "I cannot determine this" (a timeout, a
    /// connection failure) by simply throwing — the evaluator absorbs the exception as a
    /// <see cref="Fault"/> and treats the term as <see cref="TruthValue.Unknown"/> (ADR-0001); no
    /// try/catch-and-wrap boilerplate is expected of the implementation.
    /// </summary>
    /// <param name="context">The application-supplied evaluation context.</param>
    /// <param name="args">This term's arguments, validated against <see cref="Schema"/> at compile time.</param>
    /// <param name="cancellationToken">A token observed for cooperative cancellation.</param>
    /// <returns>
    /// A <see cref="ValueTask{Boolean}"/> resolving to this term's boolean answer for the given context.
    /// </returns>
    public ValueTask<bool> EvaluateAsync(TContext context, PredicateArguments args, CancellationToken cancellationToken);
}
