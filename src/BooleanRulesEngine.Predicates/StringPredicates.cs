namespace BooleanRulesEngine.Predicates;

using BooleanRulesEngine.Abstractions;

/// <summary>
/// Ready-made, generic string-comparison predicate factories, each parameterized by a
/// <c>Func&lt;TContext, string?&gt;</c> value selector supplied at registration and a single
/// <c>string</c> comparison-target argument supplied in rule text. Every predicate here uses
/// ordinal comparison only — never culture-sensitive comparison — so rule behavior never depends on
/// the host process's current culture.
/// </summary>
public static class StringPredicates
{
    /// <summary>Creates a case-sensitive (ordinal) string-equality predicate.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to compare from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the comparison target.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<bool>> Evaluate
    ) Equals<TContext>(string name, Func<TContext, string?> selector, string label = "Equals", string argumentName = "value")
    {
        const string description =
            "True when the selected string equals the argument exactly (ordinal, case-sensitive "
            + "comparison). A null selected value is treated as not-equal (false), never a fault.";
        return Create(
            name,
            label,
            description,
            selector,
            argumentName,
            "The string the selected value must equal.",
            static (selected, target) => string.Equals(selected, target, StringComparison.Ordinal)
        );
    }

    /// <summary>Creates a case-insensitive (ordinal) string-equality predicate.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to compare from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the comparison target.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<bool>> Evaluate
    ) EqualsIgnoreCase<TContext>(
        string name,
        Func<TContext, string?> selector,
        string label = "Equals (Ignore Case)",
        string argumentName = "value"
    )
    {
        const string description =
            "True when the selected string equals the argument, ignoring case. Comparison is ordinal "
            + "case-insensitive, never culture-sensitive, so behavior never depends on the host's "
            + "current culture. A null selected value is treated as not-equal (false), never a fault.";
        return Create(
            name,
            label,
            description,
            selector,
            argumentName,
            "The string the selected value must equal, ignoring case.",
            static (selected, target) => string.Equals(selected, target, StringComparison.OrdinalIgnoreCase)
        );
    }

    /// <summary>Creates an ordinal <c>StartsWith</c> predicate.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the prefix.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<bool>> Evaluate
    ) StartsWith<TContext>(
        string name,
        Func<TContext, string?> selector,
        string label = "Starts With",
        string argumentName = "value"
    )
    {
        const string description =
            "True when the selected string starts with the argument (ordinal comparison, never "
            + "culture-sensitive). A null selected value is treated as not-matching (false), never a fault.";
        return Create(
            name,
            label,
            description,
            selector,
            argumentName,
            "The prefix the selected value must start with.",
            static (selected, target) => selected.StartsWith(target, StringComparison.Ordinal)
        );
    }

    /// <summary>Creates an ordinal <c>EndsWith</c> predicate.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the suffix.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<bool>> Evaluate
    ) EndsWith<TContext>(
        string name,
        Func<TContext, string?> selector,
        string label = "Ends With",
        string argumentName = "value"
    )
    {
        const string description =
            "True when the selected string ends with the argument (ordinal comparison, never "
            + "culture-sensitive). A null selected value is treated as not-matching (false), never a fault.";
        return Create(
            name,
            label,
            description,
            selector,
            argumentName,
            "The suffix the selected value must end with.",
            static (selected, target) => selected.EndsWith(target, StringComparison.Ordinal)
        );
    }

    /// <summary>Creates an ordinal <c>Contains</c> predicate.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the substring.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<bool>> Evaluate
    ) Contains<TContext>(
        string name,
        Func<TContext, string?> selector,
        string label = "Contains",
        string argumentName = "value"
    )
    {
        const string description =
            "True when the selected string contains the argument as a substring (ordinal comparison, "
            + "never culture-sensitive). A null selected value is treated as not-matching (false), "
            + "never a fault.";
        return Create(
            name,
            label,
            description,
            selector,
            argumentName,
            "The substring the selected value must contain.",
            static (selected, target) => selected.Contains(target, StringComparison.Ordinal)
        );
    }

    /// <summary>Creates a predicate that is true when the selected string is <see langword="null"/> or <see cref="string.Empty"/>.</summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to test from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<bool>> Evaluate
    ) IsNullOrEmpty<TContext>(string name, Func<TContext, string?> selector, string label = "Is Null Or Empty")
    {
        PredicateSchema schema = PredicateSchema.NoArguments(
            name,
            label,
            "True when the selected string is null or the empty string (\"\")."
        );

        return (schema, (context, _, _) => ValueTask.FromResult(string.IsNullOrEmpty(selector(context))));
    }

    private static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<bool>> Evaluate
    ) Create<TContext>(
        string name,
        string label,
        string description,
        Func<TContext, string?> selector,
        string argumentName,
        string argumentDescription,
        Func<string, string, bool> compare
    )
    {
        PredicateSchema schema = new(
            name,
            label,
            description,
            [new PredicateArgumentSchema(argumentName, argumentDescription, LiteralKind.String)]
        );

        return (
            schema,
            (context, args, _) =>
            {
                string? selected = selector(context);
                return selected is null
                    ? ValueTask.FromResult(false)
                    : ValueTask.FromResult(compare(selected, args.GetString(argumentName)));
            }
        );
    }
}
