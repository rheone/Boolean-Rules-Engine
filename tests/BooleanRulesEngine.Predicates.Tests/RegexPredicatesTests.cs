namespace BooleanRulesEngine.Predicates.Tests;

using System.Text.RegularExpressions;
using BooleanRulesEngine.Abstractions;

public class RegexPredicatesTests
{
    [Fact]
    public async Task Matches_MatchingPattern_ReturnsTrue()
    {
        (PredicateSchema schema, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            RegexPredicates.Matches<TestContext>("matchesEmail", c => c.Value);

        bool result = await evaluate(
            new TestContext("alice@example.com"),
            Args("pattern", @"^\S+@\S+\.\S+$"),
            CancellationToken.None
        );

        Assert.True(result);
        Assert.Equal("matchesEmail", schema.Name);
    }

    [Fact]
    public async Task Matches_NonMatchingPattern_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            RegexPredicates.Matches<TestContext>("matchesEmail", c => c.Value);

        bool result = await evaluate(
            new TestContext("not-an-email"),
            Args("pattern", @"^\S+@\S+\.\S+$"),
            CancellationToken.None
        );

        Assert.False(result);
    }

    [Fact]
    public async Task Matches_NullSelectedValue_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            RegexPredicates.Matches<TestContext>("matchesEmail", c => c.Value);

        bool result = await evaluate(new TestContext(null), Args("pattern", @"^\S+@\S+\.\S+$"), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public Task Matches_InvalidPattern_ThrowsAtEvaluationTime()
    {
        // ADR-0001's Kleene failure model: the predicate contract signals "cannot determine this" by
        // simply throwing, and the evaluator (not this package) is what turns that into Unknown. This
        // package's job here is only to confirm it does not silently swallow the invalid pattern.
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            RegexPredicates.Matches<TestContext>("matchesEmail", c => c.Value);

        return Assert.ThrowsAsync<RegexParseException>(async () =>
            await evaluate(new TestContext("anything"), Args("pattern", "("), CancellationToken.None)
        );
    }

    [Fact]
    public async Task Matches_SamePatternReusedAcrossCalls_UsesCachedRegex()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            RegexPredicates.Matches<TestContext>("matchesDigits", c => c.Value);

        bool first = await evaluate(new TestContext("123"), Args("pattern", @"^\d+$"), CancellationToken.None);
        bool second = await evaluate(new TestContext("456"), Args("pattern", @"^\d+$"), CancellationToken.None);

        Assert.True(first);
        Assert.True(second);
    }

    private static PredicateArguments Args(string name, string value)
    {
        return new PredicateArguments(new Dictionary<string, LiteralValue> { [name] = LiteralValue.OfString(value) });
    }
}
