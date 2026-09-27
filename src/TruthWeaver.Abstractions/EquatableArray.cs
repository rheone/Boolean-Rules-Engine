namespace TruthWeaver.Abstractions;

/// <summary>
/// An immutable, order-sensitive, structurally-equatable list. Record types compare reference-typed
/// properties with <see cref="EqualityComparer{T}.Default"/>, which for <c>IReadOnlyList&lt;T&gt;</c>
/// backed by a plain array or <see cref="ImmutableArray{T}"/> means reference equality, not element
/// equality — this wrapper is what lets AST nodes and term arguments compare structurally, which the
/// canonical-equality and round-trip guarantees (ADR-0003) depend on.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <remarks>Initializes a new instance of the <see cref="EquatableArray{T}"/> struct.</remarks>
/// <param name="items">The elements, copied into an immutable backing array.</param>
public readonly struct EquatableArray<T>(IEnumerable<T> items) : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
{
    private readonly ImmutableArray<T> items = [.. items];

    /// <summary>Gets an empty array.</summary>
    public static EquatableArray<T> Empty { get; } = new([]);

    /// <inheritdoc />
    public int Count => this.items.IsDefault ? 0 : this.items.Length;

    /// <inheritdoc />
    public T this[int index] => this.items[index];

    /// <summary>Determines whether two arrays are structurally equal.</summary>
    /// <param name="left">The left array.</param>
    /// <param name="right">The right array.</param>
    /// <returns><see langword="true"/> if the arrays contain equal elements in the same order.</returns>
    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right)
    {
        return left.Equals(right);
    }

    /// <summary>Determines whether two arrays are not structurally equal.</summary>
    /// <param name="left">The left array.</param>
    /// <param name="right">The right array.</param>
    /// <returns><see langword="true"/> if the arrays are not structurally equal.</returns>
    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc />
    public bool Equals(EquatableArray<T> other)
    {
        ImmutableArray<T> a = this.items.IsDefault ? [] : this.items;
        ImmutableArray<T> b = other.items.IsDefault ? [] : other.items;
        return a.AsSpan().SequenceEqual(b.AsSpan(), EqualityComparer<T>.Default);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is EquatableArray<T> other && this.Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        HashCode hash = default;
        if (!this.items.IsDefault)
        {
            foreach (T item in this.items)
            {
                hash.Add(item);
            }
        }

        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)(this.items.IsDefault ? [] : this.items)).GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
