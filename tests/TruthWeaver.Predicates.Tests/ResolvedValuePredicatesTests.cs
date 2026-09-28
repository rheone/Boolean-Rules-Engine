namespace TruthWeaver.Predicates.Tests;

using TruthWeaver.Abstractions;

public class ResolvedValuePredicatesTests
{
    [Fact]
    public async Task Create_WithResolveAndTest_SchemaMatchesSuppliedValues()
    {
        (PredicateSchema schema, _) = ResolvedValuePredicates.Create<TestContext, decimal>(
            "isWithinBudget",
            "Is Within Budget",
            "Is the amount within the resolved limit?",
            (_, args, _) =>
                ValueTask.FromResult(decimal.Parse(args.GetString("limit"), System.Globalization.CultureInfo.InvariantCulture)),
            static resolved => resolved >= 100m,
            new PredicateArgumentSchema("limit", "The limit to resolve.", LiteralKind.String)
        );

        Assert.Equal("isWithinBudget", schema.Name);
        Assert.Equal("Is Within Budget", schema.Label);
        Assert.Equal("Is the amount within the resolved limit?", schema.Description);
        Assert.Equal("limit", Assert.Single(schema.Arguments).Name);
    }

    [Fact]
    public async Task Create_WithResolveAndTest_EvaluatesByComposingResolveThenTest()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            ResolvedValuePredicates.Create<TestContext, decimal>(
                "isWithinBudget",
                "Is Within Budget",
                "Is the amount within the resolved limit?",
                (_, args, _) =>
                    ValueTask.FromResult(
                        decimal.Parse(args.GetString("limit"), System.Globalization.CultureInfo.InvariantCulture)
                    ),
                static resolved => resolved >= 100m,
                new PredicateArgumentSchema("limit", "The limit to resolve.", LiteralKind.String)
            );

        bool result = await evaluate(new TestContext(null), Args("limit", "150"), CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task Create_WithResolveAndTest_TestRejectsResolvedValue_EvaluatesFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            ResolvedValuePredicates.Create<TestContext, decimal>(
                "isWithinBudget",
                "Is Within Budget",
                "Is the amount within the resolved limit?",
                (_, args, _) =>
                    ValueTask.FromResult(
                        decimal.Parse(args.GetString("limit"), System.Globalization.CultureInfo.InvariantCulture)
                    ),
                static resolved => resolved >= 100m,
                new PredicateArgumentSchema("limit", "The limit to resolve.", LiteralKind.String)
            );

        bool result = await evaluate(new TestContext(null), Args("limit", "50"), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task Create_SingleValueOverload_EvaluatesToExactlyWhatResolveReturns()
    {
        (PredicateSchema schema, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            ResolvedValuePredicates.Create<TestContext>(
                "isFeatureEnabled",
                "Is Feature Enabled",
                "Is the given feature flag enabled?",
                (_, args, _) => ValueTask.FromResult(args.GetString("flagKey") == "enabled-flag"),
                new PredicateArgumentSchema("flagKey", "The flag key to look up.", LiteralKind.String)
            );

        bool enabledResult = await evaluate(new TestContext(null), Args("flagKey", "enabled-flag"), CancellationToken.None);
        bool disabledResult = await evaluate(new TestContext(null), Args("flagKey", "other-flag"), CancellationToken.None);

        Assert.True(enabledResult);
        Assert.False(disabledResult);
        Assert.Equal("isFeatureEnabled", schema.Name);
    }

    [Fact]
    public async Task Create_WithResolveAndTest_ResolveThrows_ExceptionSurfacesUnwrapped()
    {
        InvalidOperationException expected = new("lookup failed");
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            ResolvedValuePredicates.Create<TestContext, decimal>(
                "isWithinBudget",
                "Is Within Budget",
                "Is the amount within the resolved limit?",
                (_, _, _) => throw expected,
                static _ => true,
                new PredicateArgumentSchema("limit", "The limit to resolve.", LiteralKind.String)
            );

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await evaluate(new TestContext(null), Args("limit", "1"), CancellationToken.None)
        );
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task Create_TwoPredicatesWithSameSchemaDifferentResolveClosures_RemainIndependent()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> first) = ResolvedValuePredicates.Create<
            TestContext,
            string
        >(
            "sameName",
            "Same Name",
            "Same schema, different closures.",
            (_, _, _) => ValueTask.FromResult("first"),
            static resolved => resolved == "first"
        );
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<bool>> second) = ResolvedValuePredicates.Create<
            TestContext,
            string
        >(
            "sameName",
            "Same Name",
            "Same schema, different closures.",
            (_, _, _) => ValueTask.FromResult("second"),
            static resolved => resolved == "second"
        );

        bool firstResult = await first(new TestContext(null), PredicateArguments.Empty, CancellationToken.None);
        bool secondResult = await second(new TestContext(null), PredicateArguments.Empty, CancellationToken.None);

        Assert.True(firstResult);
        Assert.True(secondResult);
    }

    private static PredicateArguments Args(string name, string value)
    {
        return new PredicateArguments(new Dictionary<string, LiteralValue> { [name] = LiteralValue.OfString(value) });
    }
}
