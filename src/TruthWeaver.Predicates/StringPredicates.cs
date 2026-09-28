namespace TruthWeaver.Predicates;

using System.Globalization;
using TruthWeaver.Abstractions;

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

    /// <summary>
    /// Creates a string-equality predicate whose case-sensitivity, culture, and whitespace-trimming
    /// behavior are rule-text arguments rather than fixed at registration. This is a deliberate
    /// divergence from every other method in this class: they are ordinal-only, fixed-behavior
    /// predicates by design (see this class's type-level remarks), so that a behavior variant is a
    /// distinct, separately-named predicate rather than a flag threaded through rule text. This method
    /// exists for the opposite case — a rule author who genuinely needs to choose case-sensitivity,
    /// culture, and trimming per rule, not per predicate name. The same divergence-for-a-reason pattern
    /// <see cref="CollectionPredicates.SetEquals{TContext}"/> already documents for a different rule.
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">Reads the string value to compare from the context.</param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the comparison target.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    /// <exception cref="CultureNotFoundException">
    /// The rule-text <c>culture</c> argument does not name a known culture. This is not caught at
    /// compile time (the schema only declares the argument's <see cref="LiteralKind"/>, not that its
    /// value must be a valid culture name); it surfaces as an evaluation-time fault
    /// (<see cref="TruthValue.Unknown"/>) per ADR-0001's Kleene failure model, the same treatment
    /// <see cref="RegexPredicates.Matches{TContext}"/> gives an invalid regular-expression pattern.
    /// </exception>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<bool>> Evaluate
    ) EqualsConfigurable<TContext>(
        string name,
        Func<TContext, string?> selector,
        string label = "Equals (Configurable)",
        string argumentName = "value"
    )
    {
        const string description =
            "True when the selected string equals the argument, under configurable comparison rules: "
            + "case-insensitive and InvariantCulture by default, both overridable, with optional "
            + "leading/trailing-whitespace trimming. A null selected value is treated as not-equal "
            + "(false), never a fault.";
        PredicateSchema schema = new(
            name,
            label,
            description,
            [
                new PredicateArgumentSchema(argumentName, "The string the selected value must equal.", LiteralKind.String),
                new PredicateArgumentSchema(
                    "ignoreCase",
                    "Whether the comparison ignores case. Defaults to true.",
                    LiteralKind.Boolean,
                    Required: false,
                    Default: LiteralValue.OfBoolean(true)
                ),
                new PredicateArgumentSchema(
                    "culture",
                    "The culture name to compare under (e.g. \"en-US\"), or empty for InvariantCulture (the default).",
                    LiteralKind.String,
                    Required: false,
                    Default: LiteralValue.OfString(string.Empty)
                ),
                new PredicateArgumentSchema(
                    "trim",
                    "Whether both sides are trimmed of leading/trailing whitespace before comparing. Defaults to false.",
                    LiteralKind.Boolean,
                    Required: false,
                    Default: LiteralValue.OfBoolean(false)
                ),
            ]
        );

        return (
            schema,
            (context, args, _) =>
            {
                string? selected = selector(context);
                if (selected is null)
                {
                    return ValueTask.FromResult(false);
                }

                string target = args.GetString(argumentName);
                bool ignoreCase = args.GetBool("ignoreCase");
                bool trim = args.GetBool("trim");
                string culture = args.GetString("culture");

                if (trim)
                {
                    selected = selected.Trim();
                    target = target.Trim();
                }

                CultureInfo compareCulture = string.IsNullOrEmpty(culture)
                    ? CultureInfo.InvariantCulture
                    : CultureInfo.GetCultureInfo(culture);
                CompareOptions options = ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None;

                return ValueTask.FromResult(compareCulture.CompareInfo.Compare(selected, target, options) == 0);
            }
        );
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
