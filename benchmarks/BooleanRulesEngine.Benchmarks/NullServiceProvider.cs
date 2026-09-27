namespace BooleanRulesEngine.Benchmarks;

/// <summary>
/// An <see cref="IServiceProvider"/> that resolves nothing. Every predicate in this suite is
/// registered as a stateless lambda (never a class-based <c>IPredicate&lt;TContext&gt;</c>), so
/// <c>CompiledRule.EvaluateAsync</c> never actually calls into this provider — it exists only
/// because <c>EvaluateAsync</c> requires one.
/// </summary>
internal sealed class NullServiceProvider : IServiceProvider
{
    /// <summary>Gets the shared instance.</summary>
    public static NullServiceProvider Instance { get; } = new();

    /// <inheritdoc />
    public object? GetService(Type serviceType)
    {
        return null;
    }
}
