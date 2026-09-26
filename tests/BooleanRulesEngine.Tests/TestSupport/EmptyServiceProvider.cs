namespace BooleanRulesEngine.Tests.TestSupport;

/// <summary>An <see cref="IServiceProvider"/> that resolves nothing — used by tests whose rules reference only lambda-registered predicates.</summary>
public sealed class EmptyServiceProvider : IServiceProvider
{
    /// <summary>Gets the shared instance.</summary>
    public static EmptyServiceProvider Instance { get; } = new();

    /// <inheritdoc />
    public object? GetService(Type serviceType)
    {
        return null;
    }
}
