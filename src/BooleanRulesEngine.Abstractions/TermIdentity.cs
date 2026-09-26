namespace BooleanRulesEngine.Abstractions;

/// <summary>
/// The identity of a term — a predicate name bound to concrete arguments — per CONTEXT.md's
/// term-identity rule. Two terms are the same identity if and only if their predicate names are
/// equal (already normalized to the registry's casing by the time a <see cref="TermIdentity"/> is
/// constructed) and their arguments, sorted by name, are pairwise equal by exact type-normalized
/// value. This is the key used for per-evaluation memoization and for the analyzer's structural
/// equality checks.
/// </summary>
/// <remarks>Initializes a new instance of the <see cref="TermIdentity"/> class.</remarks>
/// <param name="predicateName">The predicate name, normalized to the registered casing.</param>
/// <param name="arguments">The arguments, sorted by name (ordinal).</param>
public sealed class TermIdentity(string predicateName, IReadOnlyList<KeyValuePair<string, LiteralValue>> arguments)
    : IEquatable<TermIdentity>
{
    /// <summary>Gets the predicate name, normalized to the registered casing.</summary>
    public string PredicateName { get; } = predicateName;

    /// <summary>Gets the arguments, sorted by name.</summary>
    public EquatableArray<KeyValuePair<string, LiteralValue>> Arguments { get; } =
        new EquatableArray<KeyValuePair<string, LiteralValue>>(arguments.OrderBy(a => a.Key, StringComparer.Ordinal));

    /// <summary>Determines whether two term identities are equal.</summary>
    /// <param name="left">The left identity.</param>
    /// <param name="right">The right identity.</param>
    /// <returns><see langword="true"/> if the identities are equal.</returns>
    public static bool operator ==(TermIdentity? left, TermIdentity? right)
    {
        return Equals(left, right);
    }

    /// <summary>Determines whether two term identities are not equal.</summary>
    /// <param name="left">The left identity.</param>
    /// <param name="right">The right identity.</param>
    /// <returns><see langword="true"/> if the identities are not equal.</returns>
    public static bool operator !=(TermIdentity? left, TermIdentity? right)
    {
        return !Equals(left, right);
    }

    /// <inheritdoc />
    public bool Equals(TermIdentity? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(this.PredicateName, other.PredicateName, StringComparison.Ordinal)
            && this.Arguments.Equals(other.Arguments);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return this.Equals(obj as TermIdentity);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(this.PredicateName, this.Arguments);
    }

    /// <summary>Renders this term identity as it would appear in the canonical DSL (e.g. <c>hasRole(role: "Y")</c>).</summary>
    /// <returns>The canonical term text.</returns>
    public override string ToString()
    {
        if (this.Arguments.Count == 0)
        {
            return this.PredicateName;
        }

        StringBuilder builder = new StringBuilder(this.PredicateName).Append('(');
        for (int i = 0; i < this.Arguments.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            KeyValuePair<string, LiteralValue> arg = this.Arguments[i];
            builder.Append(arg.Key).Append(": ").Append(arg.Value);
        }

        return builder.Append(')').ToString();
    }
}
