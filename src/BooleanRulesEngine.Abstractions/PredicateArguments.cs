namespace BooleanRulesEngine.Abstractions;

/// <summary>
/// A small, non-generic accessor a predicate uses to read its own arguments inside
/// <see cref="IPredicate{TContext}.EvaluateAsync"/>. Argument presence and type are validated
/// against the predicate's <see cref="PredicateSchema"/> at compile time (ADR-0002), so a missing or
/// mistyped argument is a compile diagnostic, never a runtime failure here — these accessors throw
/// only if a predicate asks for an argument name or type its own schema never declared, which is an
/// authoring bug in the predicate itself, not a rule-authoring error.
/// </summary>
/// <remarks>Initializes a new instance of the <see cref="PredicateArguments"/> class.</remarks>
/// <param name="values">The argument values, keyed by name (ordinal, case-sensitive — argument names in a schema are exact).</param>
public sealed class PredicateArguments(IReadOnlyDictionary<string, LiteralValue> values)
{
    private readonly IReadOnlyDictionary<string, LiteralValue> values = values;

    /// <summary>Gets an empty argument set, for zero-argument terms.</summary>
    public static PredicateArguments Empty { get; } = new(new Dictionary<string, LiteralValue>());

    /// <summary>Gets a <see cref="string"/> argument.</summary>
    /// <param name="name">The argument name.</param>
    /// <returns>The string value.</returns>
    public string GetString(string name)
    {
        return this.Get(name, LiteralKind.String).AsString();
    }

    /// <summary>Gets a <see cref="long"/> argument.</summary>
    /// <param name="name">The argument name.</param>
    /// <returns>The integer value.</returns>
    public long GetInt64(string name)
    {
        return this.Get(name, LiteralKind.Int64).AsInt64();
    }

    /// <summary>Gets a <see cref="decimal"/> argument.</summary>
    /// <param name="name">The argument name.</param>
    /// <returns>The decimal value.</returns>
    public decimal GetDecimal(string name)
    {
        return this.Get(name, LiteralKind.Decimal).AsDecimal();
    }

    /// <summary>Gets a <see cref="bool"/> argument.</summary>
    /// <param name="name">The argument name.</param>
    /// <returns>The boolean value.</returns>
    public bool GetBool(string name)
    {
        return this.Get(name, LiteralKind.Boolean).AsBoolean();
    }

    /// <summary>Gets a <see cref="DateTimeOffset"/> argument.</summary>
    /// <param name="name">The argument name.</param>
    /// <returns>The date/time value.</returns>
    public DateTimeOffset GetDateTimeOffset(string name)
    {
        return this.Get(name, LiteralKind.DateTimeOffset).AsDateTimeOffset();
    }

    /// <summary>Gets a <see cref="string"/> array argument.</summary>
    /// <param name="name">The argument name.</param>
    /// <returns>The array elements.</returns>
    public IReadOnlyList<string> GetStringArray(string name)
    {
        return [.. this.Get(name, LiteralKind.StringArray).AsArray().Select(v => v.AsString())];
    }

    /// <summary>Gets a <see cref="long"/> array argument.</summary>
    /// <param name="name">The argument name.</param>
    /// <returns>The array elements.</returns>
    public IReadOnlyList<long> GetInt64Array(string name)
    {
        return [.. this.Get(name, LiteralKind.Int64Array).AsArray().Select(v => v.AsInt64())];
    }

    /// <summary>Gets a <see cref="decimal"/> array argument.</summary>
    /// <param name="name">The argument name.</param>
    /// <returns>The array elements.</returns>
    public IReadOnlyList<decimal> GetDecimalArray(string name)
    {
        return [.. this.Get(name, LiteralKind.DecimalArray).AsArray().Select(v => v.AsDecimal())];
    }

    /// <summary>Gets a <see cref="bool"/> array argument.</summary>
    /// <param name="name">The argument name.</param>
    /// <returns>The array elements.</returns>
    public IReadOnlyList<bool> GetBoolArray(string name)
    {
        return [.. this.Get(name, LiteralKind.BooleanArray).AsArray().Select(v => v.AsBoolean())];
    }

    /// <summary>Gets a <see cref="DateTimeOffset"/> array argument.</summary>
    /// <param name="name">The argument name.</param>
    /// <returns>The array elements.</returns>
    public IReadOnlyList<DateTimeOffset> GetDateTimeOffsetArray(string name)
    {
        return [.. this.Get(name, LiteralKind.DateTimeOffsetArray).AsArray().Select(v => v.AsDateTimeOffset())];
    }

    /// <summary>Gets the raw literal value for an argument, regardless of kind.</summary>
    /// <param name="name">The argument name.</param>
    /// <returns>The literal value.</returns>
    /// <exception cref="KeyNotFoundException">No argument named <paramref name="name"/> was supplied.</exception>
    public LiteralValue GetRaw(string name)
    {
        return this.values.TryGetValue(name, out LiteralValue value)
            ? value
            : throw new KeyNotFoundException($"No argument named '{name}' was supplied to this term.");
    }

    private LiteralValue Get(string name, LiteralKind expectedKind)
    {
        LiteralValue value = this.GetRaw(name);
        if (value.Kind != expectedKind)
        {
            throw new InvalidOperationException(
                $"Argument '{name}' is of kind '{value.Kind}', not the requested '{expectedKind}'."
            );
        }

        return value;
    }
}
