namespace TruthWeaver.Predicates.Tests;

using TruthWeaver.Abstractions;

public class CollectionPredicatesTests
{
    [Fact]
    public async Task SetEquals_SameElementsDifferentOrder_ReturnsTrue()
    {
        (PredicateSchema schema, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            CollectionPredicates.SetEquals<TestContext>("setEquals", c => c.Values);

        TruthValue result = await evaluate(
            new TestContext(null, ["b", "a", "c"]),
            Args("values", "a", "b", "c"),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.True, result);
        Assert.Equal("setEquals", schema.Name);
    }

    [Fact]
    public async Task SetEquals_DuplicatesIgnored_ReturnsTrue()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            CollectionPredicates.SetEquals<TestContext>("setEquals", c => c.Values);

        TruthValue result = await evaluate(
            new TestContext(null, ["a", "a", "b"]),
            Args("values", "a", "b", "b"),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.True, result);
    }

    [Fact]
    public async Task SetEquals_DifferingElements_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            CollectionPredicates.SetEquals<TestContext>("setEquals", c => c.Values);

        TruthValue result = await evaluate(new TestContext(null, ["a", "b"]), Args("values", "a", "c"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    [Fact]
    public async Task SetEquals_DifferingCase_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            CollectionPredicates.SetEquals<TestContext>("setEquals", c => c.Values);

        TruthValue result = await evaluate(new TestContext(null, ["Alice"]), Args("values", "alice"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    [Fact]
    public async Task SetEquals_EmptySelectedAndEmptyLiteral_ReturnsTrue()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            CollectionPredicates.SetEquals<TestContext>("setEquals", c => c.Values);

        TruthValue result = await evaluate(new TestContext(null, []), Args("values"), CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
    }

    [Fact]
    public async Task SetEquals_NullSelectedCollectionTreatedAsEmpty_MatchesEmptyLiteral()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            CollectionPredicates.SetEquals<TestContext>("setEquals", c => c.Values);

        TruthValue result = await evaluate(new TestContext(null, null), Args("values"), CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
    }

    [Fact]
    public async Task SetEquals_NullSelectedCollection_DoesNotMatchNonEmptyLiteral()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            CollectionPredicates.SetEquals<TestContext>("setEquals", c => c.Values);

        TruthValue result = await evaluate(new TestContext(null, null), Args("values", "a"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    private static PredicateArguments Args(string name, params string[] values)
    {
        LiteralValue array = LiteralValue.OfArray(LiteralKind.String, values.Select(LiteralValue.OfString));
        return new PredicateArguments(new Dictionary<string, LiteralValue> { [name] = array });
    }
}
