namespace TruthWeaver.Predicates;

using TruthWeaver.Abstractions;

/// <summary>
/// Ready-made, generic collection predicate factories, each parameterized by a selector supplied at
/// registration.
/// </summary>
public static class CollectionPredicates
{
    /// <summary>
    /// Creates an order-insensitive, duplicate-insensitive set-equality predicate: true when the
    /// selected collection and the literal array argument contain the same distinct elements. This is
    /// a deliberate divergence from CONTEXT.md's "array-valued arguments are order-sensitive"
    /// term-identity rule — that rule governs *term identity* (whether two terms are the same
    /// variable for memoization/canonical-equality purposes), not this predicate's own *evaluation*
    /// semantics, so it is not a contradiction of that rule. Comparison is case-sensitive (ordinal),
    /// consistent with CONTEXT.md's general "argument values are case-sensitive" stance; this ticket
    /// does not provide a case-insensitive variant.
    /// </summary>
    /// <typeparam name="TContext">The application context type the selector reads from.</typeparam>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="selector">
    /// Reads the collection to compare from the context. A <see langword="null"/> result is treated as
    /// an empty collection, never a fault.
    /// </param>
    /// <param name="label">A short, human-friendly display name for this predicate.</param>
    /// <param name="argumentName">The rule-text argument name for the comparison set.</param>
    /// <returns>The predicate's schema and stateless evaluation delegate, ready for <c>PredicateRegistryBuilder&lt;TContext&gt;.Add</c>.</returns>
    public static (
        PredicateSchema Schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate
    ) SetEquals<TContext>(
        string name,
        Func<TContext, IReadOnlyCollection<string>?> selector,
        string label = "Set Equals",
        string argumentName = "values"
    )
    {
        const string description =
            "True when the selected collection and the argument array contain the same elements, "
            + "ignoring order and duplicates (set semantics, not sequence semantics — a deliberate "
            + "divergence from CONTEXT.md's array-argument order-sensitive term-identity rule, which "
            + "governs term identity, not this predicate's evaluation semantics). Comparison is "
            + "case-sensitive (ordinal); no case-insensitive variant is provided. A null selected "
            + "collection is treated as empty, never a fault.";
        PredicateSchema schema = new(
            name,
            label,
            description,
            [new PredicateArgumentSchema(argumentName, "The set of string values to compare against.", LiteralKind.StringArray)]
        );

        return (
            schema,
            (context, args, _) =>
            {
                IReadOnlyCollection<string>? selected = selector(context);
                HashSet<string> selectedSet = selected is null
                    ? new(StringComparer.Ordinal)
                    : new(selected, StringComparer.Ordinal);
                HashSet<string> targetSet = new(args.GetStringArray(argumentName), StringComparer.Ordinal);
                return PredicateResult.FromBoolAsync(selectedSet.SetEquals(targetSet));
            }
        );
    }
}
