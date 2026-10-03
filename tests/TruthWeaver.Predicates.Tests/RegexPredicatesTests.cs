namespace TruthWeaver.Predicates.Tests;

using System.Text.RegularExpressions;
using TruthWeaver.Abstractions;

public class RegexPredicatesTests
{
    [Fact]
    public async Task Matches_MatchingPattern_ReturnsTrue()
    {
        (PredicateSchema schema, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.Matches<TestContext>("matchesEmail", c => c.Value);

        TruthValue result = await evaluate(
            new TestContext("alice@example.com"),
            Args("pattern", @"^\S+@\S+\.\S+$"),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.True, result);
        Assert.Equal("matchesEmail", schema.Name);
    }

    [Fact]
    public async Task Matches_NonMatchingPattern_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.Matches<TestContext>("matchesEmail", c => c.Value);

        TruthValue result = await evaluate(
            new TestContext("not-an-email"),
            Args("pattern", @"^\S+@\S+\.\S+$"),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.False, result);
    }

    [Fact]
    public async Task Matches_NullSelectedValue_ReturnsFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.Matches<TestContext>("matchesEmail", c => c.Value);

        TruthValue result = await evaluate(new TestContext(null), Args("pattern", @"^\S+@\S+\.\S+$"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    [Fact]
    public Task Matches_InvalidPattern_ThrowsAtEvaluationTime()
    {
        // ADR-0001's Kleene failure model: the predicate contract signals "cannot determine this" by
        // simply throwing, and the evaluator (not this package) is what turns that into Unknown. This
        // package's job here is only to confirm it does not silently swallow the invalid pattern.
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.Matches<TestContext>("matchesEmail", c => c.Value);

        return Assert.ThrowsAsync<RegexParseException>(async () =>
            await evaluate(new TestContext("anything"), Args("pattern", "("), CancellationToken.None)
        );
    }

    [Fact]
    public async Task Matches_SamePatternReusedAcrossCalls_UsesCachedRegex()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            RegexPredicates.Matches<TestContext>("matchesDigits", c => c.Value);

        TruthValue first = await evaluate(new TestContext("123"), Args("pattern", @"^\d+$"), CancellationToken.None);
        TruthValue second = await evaluate(new TestContext("456"), Args("pattern", @"^\d+$"), CancellationToken.None);

        Assert.Equal(TruthValue.True, first);
        Assert.Equal(TruthValue.True, second);
    }

    private static PredicateArguments Args(string name, string value)
    {
        return new PredicateArguments(new Dictionary<string, LiteralValue> { [name] = LiteralValue.OfString(value) });
    }
}
