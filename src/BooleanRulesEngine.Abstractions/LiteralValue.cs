namespace BooleanRulesEngine.Abstractions;

/// <summary>
/// A single argument value drawn from the closed literal type set (ADR-0003). Scalar comparison is
/// exact and case-sensitive for strings (CONTEXT.md's term-identity rule: argument values are
/// case-sensitive, since role codes and similar are frequently case-significant). Array comparison is
/// order-sensitive.
/// </summary>
public readonly struct LiteralValue : IEquatable<LiteralValue>
{
    private readonly string? stringValue;
    private readonly long int64Value;
    private readonly decimal decimalValue;
    private readonly bool booleanValue;
    private readonly DateTimeOffset dateTimeOffsetValue;
    private readonly Guid guidValue;
    private readonly EquatableArray<LiteralValue> arrayValue;

    private LiteralValue(
        LiteralKind kind,
        string? stringValue,
        long int64Value,
        decimal decimalValue,
        bool booleanValue,
        DateTimeOffset dateTimeOffsetValue,
        Guid guidValue,
        EquatableArray<LiteralValue> arrayValue
    )
    {
        this.Kind = kind;
        this.stringValue = stringValue;
        this.int64Value = int64Value;
        this.decimalValue = decimalValue;
        this.booleanValue = booleanValue;
        this.dateTimeOffsetValue = dateTimeOffsetValue;
        this.guidValue = guidValue;
        this.arrayValue = arrayValue;
    }

    /// <summary>Gets the runtime kind of this value.</summary>
    public LiteralKind Kind { get; }

    /// <summary>Gets a value indicating whether <see cref="Kind"/> is one of the array kinds.</summary>
    public bool IsArray =>
        this.Kind
            is LiteralKind.StringArray
                or LiteralKind.Int64Array
                or LiteralKind.DecimalArray
                or LiteralKind.BooleanArray
                or LiteralKind.DateTimeOffsetArray
                or LiteralKind.GuidArray;

    /// <summary>Determines whether two literals are equal by kind and value.</summary>
    /// <param name="left">The left literal.</param>
    /// <param name="right">The right literal.</param>
    /// <returns><see langword="true"/> if the literals are equal.</returns>
    public static bool operator ==(LiteralValue left, LiteralValue right)
    {
        return left.Equals(right);
    }

    /// <summary>Determines whether two literals are not equal by kind and value.</summary>
    /// <param name="left">The left literal.</param>
    /// <param name="right">The right literal.</param>
    /// <returns><see langword="true"/> if the literals are not equal.</returns>
    public static bool operator !=(LiteralValue left, LiteralValue right)
    {
        return !left.Equals(right);
    }

    /// <summary>Creates a <see cref="LiteralKind.String"/> value.</summary>
    /// <param name="value">The string value.</param>
    /// <returns>The wrapped literal.</returns>
    public static LiteralValue OfString(string value)
    {
        return new(LiteralKind.String, value, default, default, default, default, Guid.Empty, default);
    }

    /// <summary>Creates a <see cref="LiteralKind.Int64"/> value.</summary>
    /// <param name="value">The integer value.</param>
    /// <returns>The wrapped literal.</returns>
    public static LiteralValue OfInt64(long value)
    {
        return new(LiteralKind.Int64, default, value, default, default, default, Guid.Empty, default);
    }

    /// <summary>Creates a <see cref="LiteralKind.Decimal"/> value.</summary>
    /// <param name="value">The decimal value.</param>
    /// <returns>The wrapped literal.</returns>
    public static LiteralValue OfDecimal(decimal value)
    {
        return new(LiteralKind.Decimal, default, default, value, default, default, Guid.Empty, default);
    }

    /// <summary>Creates a <see cref="LiteralKind.Boolean"/> value.</summary>
    /// <param name="value">The boolean value.</param>
    /// <returns>The wrapped literal.</returns>
    public static LiteralValue OfBoolean(bool value)
    {
        return new(LiteralKind.Boolean, default, default, default, value, default, Guid.Empty, default);
    }

    /// <summary>Creates a <see cref="LiteralKind.DateTimeOffset"/> value.</summary>
    /// <param name="value">The date/time value.</param>
    /// <returns>The wrapped literal.</returns>
    public static LiteralValue OfDateTimeOffset(DateTimeOffset value)
    {
        return new(LiteralKind.DateTimeOffset, default, default, default, default, value, Guid.Empty, default);
    }

    /// <summary>Creates a <see cref="LiteralKind.Guid"/> value.</summary>
    /// <param name="value">The GUID value.</param>
    /// <returns>The wrapped literal.</returns>
    public static LiteralValue OfGuid(Guid value)
    {
        return new(LiteralKind.Guid, default, default, default, default, default, value, default);
    }

    /// <summary>Creates an array literal of the given element kind.</summary>
    /// <param name="elementKind">The scalar kind every element must have.</param>
    /// <param name="elements">The elements, in source order (arrays are order-sensitive for term identity).</param>
    /// <returns>The wrapped array literal.</returns>
    /// <exception cref="ArgumentException"><paramref name="elementKind"/> is itself an array kind, or an element does not match it.</exception>
    public static LiteralValue OfArray(LiteralKind elementKind, IEnumerable<LiteralValue> elements)
    {
        LiteralKind arrayKind = ToArrayKind(elementKind);
        EquatableArray<LiteralValue> items = new(elements);
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].Kind != elementKind)
            {
                throw new ArgumentException(
                    $"Array element of kind '{items[i].Kind}' does not match declared element kind '{elementKind}'.",
                    nameof(elements)
                );
            }
        }

        return new LiteralValue(arrayKind, default, default, default, default, default, Guid.Empty, items);
    }

    /// <summary>Maps a scalar <see cref="LiteralKind"/> to its corresponding array kind.</summary>
    /// <param name="elementKind">The scalar element kind.</param>
    /// <returns>The array kind whose elements are <paramref name="elementKind"/>.</returns>
    public static LiteralKind ToArrayKind(LiteralKind elementKind)
    {
        return elementKind switch
        {
            LiteralKind.String => LiteralKind.StringArray,
            LiteralKind.Int64 => LiteralKind.Int64Array,
            LiteralKind.Decimal => LiteralKind.DecimalArray,
            LiteralKind.Boolean => LiteralKind.BooleanArray,
            LiteralKind.DateTimeOffset => LiteralKind.DateTimeOffsetArray,
            LiteralKind.Guid => LiteralKind.GuidArray,
            _ => throw new ArgumentException($"'{elementKind}' is not a scalar literal kind.", nameof(elementKind)),
        };
    }

    /// <summary>Maps an array <see cref="LiteralKind"/> to the kind of its elements.</summary>
    /// <param name="arrayKind">The array kind.</param>
    /// <returns>The scalar element kind.</returns>
    public static LiteralKind ToElementKind(LiteralKind arrayKind)
    {
        return arrayKind switch
        {
            LiteralKind.StringArray => LiteralKind.String,
            LiteralKind.Int64Array => LiteralKind.Int64,
            LiteralKind.DecimalArray => LiteralKind.Decimal,
            LiteralKind.BooleanArray => LiteralKind.Boolean,
            LiteralKind.DateTimeOffsetArray => LiteralKind.DateTimeOffset,
            LiteralKind.GuidArray => LiteralKind.Guid,
            _ => throw new ArgumentException($"'{arrayKind}' is not an array literal kind.", nameof(arrayKind)),
        };
    }

    /// <summary>Gets the wrapped <see cref="string"/> value.</summary>
    /// <returns>The string value.</returns>
    /// <exception cref="InvalidOperationException"><see cref="Kind"/> is not <see cref="LiteralKind.String"/>.</exception>
    public string AsString()
    {
        return this.Kind == LiteralKind.String ? this.stringValue! : throw WrongKind(LiteralKind.String, this.Kind);
    }

    /// <summary>Gets the wrapped <see cref="long"/> value.</summary>
    /// <returns>The integer value.</returns>
    /// <exception cref="InvalidOperationException"><see cref="Kind"/> is not <see cref="LiteralKind.Int64"/>.</exception>
    public long AsInt64()
    {
        return this.Kind == LiteralKind.Int64 ? this.int64Value : throw WrongKind(LiteralKind.Int64, this.Kind);
    }

    /// <summary>Gets the wrapped <see cref="decimal"/> value.</summary>
    /// <returns>The decimal value.</returns>
    /// <exception cref="InvalidOperationException"><see cref="Kind"/> is not <see cref="LiteralKind.Decimal"/>.</exception>
    public decimal AsDecimal()
    {
        return this.Kind == LiteralKind.Decimal ? this.decimalValue : throw WrongKind(LiteralKind.Decimal, this.Kind);
    }

    /// <summary>Gets the wrapped <see cref="bool"/> value.</summary>
    /// <returns>The boolean value.</returns>
    /// <exception cref="InvalidOperationException"><see cref="Kind"/> is not <see cref="LiteralKind.Boolean"/>.</exception>
    public bool AsBoolean()
    {
        return this.Kind == LiteralKind.Boolean ? this.booleanValue : throw WrongKind(LiteralKind.Boolean, this.Kind);
    }

    /// <summary>Gets the wrapped <see cref="DateTimeOffset"/> value.</summary>
    /// <returns>The date/time value.</returns>
    /// <exception cref="InvalidOperationException"><see cref="Kind"/> is not <see cref="LiteralKind.DateTimeOffset"/>.</exception>
    public DateTimeOffset AsDateTimeOffset()
    {
        return this.Kind == LiteralKind.DateTimeOffset
            ? this.dateTimeOffsetValue
            : throw WrongKind(LiteralKind.DateTimeOffset, this.Kind);
    }

    /// <summary>Gets the wrapped <see cref="Guid"/> value.</summary>
    /// <returns>The GUID value.</returns>
    /// <exception cref="InvalidOperationException"><see cref="Kind"/> is not <see cref="LiteralKind.Guid"/>.</exception>
    public Guid AsGuid()
    {
        return this.Kind == LiteralKind.Guid ? this.guidValue : throw WrongKind(LiteralKind.Guid, this.Kind);
    }

    /// <summary>Gets the wrapped array's elements.</summary>
    /// <returns>The array elements, in order.</returns>
    /// <exception cref="InvalidOperationException"><see cref="Kind"/> is not an array kind.</exception>
    public EquatableArray<LiteralValue> AsArray()
    {
        return this.IsArray
            ? this.arrayValue
            : throw new InvalidOperationException($"Value of kind '{this.Kind}' is not an array.");
    }

    /// <inheritdoc />
    public bool Equals(LiteralValue other)
    {
        if (this.Kind != other.Kind)
        {
            return false;
        }

        return this.Kind switch
        {
            LiteralKind.String => string.Equals(this.stringValue, other.stringValue, StringComparison.Ordinal),
            LiteralKind.Int64 => this.int64Value == other.int64Value,
            LiteralKind.Decimal => this.decimalValue == other.decimalValue,
            LiteralKind.Boolean => this.booleanValue == other.booleanValue,
            LiteralKind.DateTimeOffset => this.dateTimeOffsetValue == other.dateTimeOffsetValue,
            LiteralKind.Guid => this.guidValue == other.guidValue,
            _ => this.arrayValue.Equals(other.arrayValue),
        };
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is LiteralValue other && this.Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return this.Kind switch
        {
            LiteralKind.String => HashCode.Combine(this.Kind, this.stringValue),
            LiteralKind.Int64 => HashCode.Combine(this.Kind, this.int64Value),
            LiteralKind.Decimal => HashCode.Combine(this.Kind, this.decimalValue),
            LiteralKind.Boolean => HashCode.Combine(this.Kind, this.booleanValue),
            LiteralKind.DateTimeOffset => HashCode.Combine(this.Kind, this.dateTimeOffsetValue),
            LiteralKind.Guid => HashCode.Combine(this.Kind, this.guidValue),
            _ => HashCode.Combine(this.Kind, this.arrayValue),
        };
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return this.Kind switch
        {
            LiteralKind.String => $"\"{EscapeForDsl(this.stringValue!)}\"",
            LiteralKind.Int64 => this.int64Value.ToString(CultureInfo.InvariantCulture),
            LiteralKind.Decimal => this.decimalValue.ToString(CultureInfo.InvariantCulture),
            LiteralKind.Boolean => this.booleanValue ? "true" : "false",
            LiteralKind.DateTimeOffset => $"\"{this.dateTimeOffsetValue.ToString("O", CultureInfo.InvariantCulture)}\"",
            LiteralKind.Guid => $"\"{this.guidValue}\"",
            _ => "[" + string.Join(", ", this.arrayValue.Select(v => v.ToString())) + "]",
        };
    }

    /// <summary>Escapes <c>\</c> and <c>"</c> so the result is valid inside a DSL quoted-string literal.</summary>
    /// <param name="value">The raw string value.</param>
    /// <returns>The escaped text, unquoted.</returns>
    private static string EscapeForDsl(string value)
    {
        return value.Contains('\\') || value.Contains('"') ? value.Replace("\\", "\\\\").Replace("\"", "\\\"") : value;
    }

    private static InvalidOperationException WrongKind(LiteralKind expected, LiteralKind actual)
    {
        return new($"Expected a literal of kind '{expected}' but found '{actual}'.");
    }
}
