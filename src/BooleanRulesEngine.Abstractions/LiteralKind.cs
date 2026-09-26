namespace BooleanRulesEngine.Abstractions;

/// <summary>
/// The closed set of literal argument-value types a rule may use (ADR-0003): scalars, and arrays of
/// each scalar. There is no context-path/handlebar syntax — a rule argument is always one of these.
/// </summary>
public enum LiteralKind
{
    /// <summary>A <see cref="string"/> value.</summary>
    String,

    /// <summary>A <see cref="long"/> value.</summary>
    Int64,

    /// <summary>A <see cref="decimal"/> value.</summary>
    Decimal,

    /// <summary>A <see cref="bool"/> value.</summary>
    Boolean,

    /// <summary>A <see cref="DateTimeOffset"/> value.</summary>
    DateTimeOffset,

    /// <summary>An array of <see cref="string"/> values.</summary>
    StringArray,

    /// <summary>An array of <see cref="long"/> values.</summary>
    Int64Array,

    /// <summary>An array of <see cref="decimal"/> values.</summary>
    DecimalArray,

    /// <summary>An array of <see cref="bool"/> values.</summary>
    BooleanArray,

    /// <summary>An array of <see cref="DateTimeOffset"/> values.</summary>
    DateTimeOffsetArray,
}
