namespace TruthWeaver.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TruthWeaver.Compilation;
using TruthWeaver.Registry;

/// <summary>
/// <see cref="IServiceCollection"/> extensions for wiring a <see cref="PredicateRegistry{TContext}"/>
/// and <see cref="RuleCompiler{TContext}"/> into a host application's container (ADR-0002/ADR-0004).
/// Registration remains explicit — this only wires the container, it does not scan assemblies for
/// predicates.
/// </summary>
public static class TruthWeaverServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="PredicateRegistry{TContext}"/> (built once, from <paramref name="configureRegistry"/>)
    /// and a <see cref="RuleCompiler{TContext}"/> as singletons.
    /// </summary>
    /// <typeparam name="TContext">The application context type predicates read from.</typeparam>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configureRegistry">Builds the predicate registry via explicit registration.</param>
    /// <param name="options">Compiler resource limits and mode, or <see langword="null"/> for the defaults.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddBooleanRulesEngine<TContext>(
        this IServiceCollection services,
        Action<PredicateRegistryBuilder<TContext>> configureRegistry,
        CompilerOptions? options = null
    )
    {
        PredicateRegistryBuilder<TContext> builder = PredicateRegistry<TContext>.CreateBuilder();
        configureRegistry(builder);
        PredicateRegistry<TContext> registry = builder.Build();

        services.AddSingleton(registry);
        services.AddSingleton(sp => new RuleCompiler<TContext>(
            registry,
            options,
            sp.GetService<ILogger<RuleCompiler<TContext>>>()
        ));
        return services;
    }
}
