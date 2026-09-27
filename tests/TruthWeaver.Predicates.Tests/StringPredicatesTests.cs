namespace TruthWeaver.Predicates.Tests;

using TruthWeaver.Abstractions;

public class StringPredicatesTests
{
    [Fact]
    public async Task Equals_MatchingCase_ReturnsTrue()
    {
        (PredicateSchema schema, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            StringPredicates.Equals<TestContext>("equalsName", c => c.Value);

        bool result = await evaluate(new TestContext("Alice"), Args("value", "Alice"), CancellationToken.None);

        Assert.True(result);
        Assert.Equal("equalsName", schema.Name);
        Assert.NotEmpty(schema.Label);
        Assert.NotEmpty(schema.Description);
    }

    [Fact]
    public async Task Equals_NonMatchingCase_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            StringPredicates.Equals<TestContext>("equalsName", c => c.Value);

        bool result = await evaluate(new TestContext("Alice"), Args("value", "alice"), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task Equals_NullSelectedValue_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            StringPredicates.Equals<TestContext>("equalsName", c => c.Value);

        bool result = await evaluate(new TestContext(null), Args("value", "Alice"), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task EqualsIgnoreCase_MatchingDifferentCase_ReturnsTrue()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            StringPredicates.EqualsIgnoreCase<TestContext>("equalsIgnoreCase", c => c.Value);

        bool result = await evaluate(new TestContext("Alice"), Args("value", "ALICE"), CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task EqualsIgnoreCase_NonMatching_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            StringPredicates.EqualsIgnoreCase<TestContext>("equalsIgnoreCase", c => c.Value);

        bool result = await evaluate(new TestContext("Alice"), Args("value", "Bob"), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task EqualsIgnoreCase_NullSelectedValue_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            StringPredicates.EqualsIgnoreCase<TestContext>("equalsIgnoreCase", c => c.Value);

        bool result = await evaluate(new TestContext(null), Args("value", "Alice"), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task StartsWith_MatchingPrefix_ReturnsTrue()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            StringPredicates.StartsWith<TestContext>("startsWith", c => c.Value);

        bool result = await evaluate(new TestContext("Alice Smith"), Args("value", "Alice"), CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task StartsWith_NonMatchingPrefix_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            StringPredicates.StartsWith<TestContext>("startsWith", c => c.Value);

        bool result = await evaluate(new TestContext("Alice Smith"), Args("value", "Bob"), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task StartsWith_NullSelectedValue_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            StringPredicates.StartsWith<TestContext>("startsWith", c => c.Value);

        bool result = await evaluate(new TestContext(null), Args("value", "Alice"), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task EndsWith_MatchingSuffix_ReturnsTrue()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            StringPredicates.EndsWith<TestContext>("endsWith", c => c.Value);

        bool result = await evaluate(new TestContext("Alice Smith"), Args("value", "Smith"), CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task EndsWith_NonMatchingSuffix_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            StringPredicates.EndsWith<TestContext>("endsWith", c => c.Value);

        bool result = await evaluate(new TestContext("Alice Smith"), Args("value", "Jones"), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task EndsWith_NullSelectedValue_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            StringPredicates.EndsWith<TestContext>("endsWith", c => c.Value);

        bool result = await evaluate(new TestContext(null), Args("value", "Smith"), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task Contains_MatchingSubstring_ReturnsTrue()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            StringPredicates.Contains<TestContext>("contains", c => c.Value);

        bool result = await evaluate(new TestContext("Alice Smith"), Args("value", "ce Sm"), CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task Contains_NonMatchingSubstring_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            StringPredicates.Contains<TestContext>("contains", c => c.Value);

        bool result = await evaluate(new TestContext("Alice Smith"), Args("value", "Bob"), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task Contains_NullSelectedValue_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            StringPredicates.Contains<TestContext>("contains", c => c.Value);

        bool result = await evaluate(new TestContext(null), Args("value", "Alice"), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task IsNullOrEmpty_NullValue_ReturnsTrue()
    {
        (PredicateSchema schema, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            StringPredicates.IsNullOrEmpty<TestContext>("isNullOrEmpty", c => c.Value);

        bool result = await evaluate(new TestContext(null), PredicateArguments.Empty, CancellationToken.None);

        Assert.True(result);
        Assert.Empty(schema.Arguments);
    }

    [Fact]
    public async Task IsNullOrEmpty_EmptyValue_ReturnsTrue()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            StringPredicates.IsNullOrEmpty<TestContext>("isNullOrEmpty", c => c.Value);

        bool result = await evaluate(new TestContext(string.Empty), PredicateArguments.Empty, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task IsNullOrEmpty_NonEmptyValue_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            StringPredicates.IsNullOrEmpty<TestContext>("isNullOrEmpty", c => c.Value);

        bool result = await evaluate(new TestContext("Alice"), PredicateArguments.Empty, CancellationToken.None);

        Assert.False(result);
    }

    private static PredicateArguments Args(string name, string value)
    {
        return new PredicateArguments(new Dictionary<string, LiteralValue> { [name] = LiteralValue.OfString(value) });
    }
}
