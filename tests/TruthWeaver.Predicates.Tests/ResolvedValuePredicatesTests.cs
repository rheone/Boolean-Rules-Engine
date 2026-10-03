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
            static resolved => resolved >= 100m ? TruthValue.True : TruthValue.False,
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
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            ResolvedValuePredicates.Create<TestContext, decimal>(
                "isWithinBudget",
                "Is Within Budget",
                "Is the amount within the resolved limit?",
                (_, args, _) =>
                    ValueTask.FromResult(
                        decimal.Parse(args.GetString("limit"), System.Globalization.CultureInfo.InvariantCulture)
                    ),
                static resolved => resolved >= 100m ? TruthValue.True : TruthValue.False,
                new PredicateArgumentSchema("limit", "The limit to resolve.", LiteralKind.String)
            );

        TruthValue result = await evaluate(new TestContext(null), Args("limit", "150"), CancellationToken.None);

        Assert.Equal(TruthValue.True, result);
    }

    [Fact]
    public async Task Create_WithResolveAndTest_TestRejectsResolvedValue_EvaluatesFalse()
    {
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            ResolvedValuePredicates.Create<TestContext, decimal>(
                "isWithinBudget",
                "Is Within Budget",
                "Is the amount within the resolved limit?",
                (_, args, _) =>
                    ValueTask.FromResult(
                        decimal.Parse(args.GetString("limit"), System.Globalization.CultureInfo.InvariantCulture)
                    ),
                static resolved => resolved >= 100m ? TruthValue.True : TruthValue.False,
                new PredicateArgumentSchema("limit", "The limit to resolve.", LiteralKind.String)
            );

        TruthValue result = await evaluate(new TestContext(null), Args("limit", "50"), CancellationToken.None);

        Assert.Equal(TruthValue.False, result);
    }

    /// <summary>A test delegate that cannot decide from the resolved value may answer Unknown, and that answer passes through unchanged.</summary>
    [Fact]
    public async Task Create_WithResolveAndTest_TestReturnsUnknown_EvaluatesUnknown_Test()
    {
        // Arrange
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            ResolvedValuePredicates.Create<TestContext, decimal?>(
                "isWithinBudget",
                "Is Within Budget",
                "Is the amount within the resolved limit?",
                (_, _, _) => ValueTask.FromResult<decimal?>(null),
                static resolved => resolved is null ? TruthValue.Unknown : TruthValue.True
            );

        // Act
        TruthValue result = await evaluate(new TestContext(null), PredicateArguments.Empty, CancellationToken.None);

        // Assert
        Assert.Equal(TruthValue.Unknown, result);
    }

    [Fact]
    public async Task Create_SingleValueOverload_EvaluatesToExactlyWhatResolveReturns()
    {
        (PredicateSchema schema, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            ResolvedValuePredicates.Create<TestContext>(
                "isFeatureEnabled",
                "Is Feature Enabled",
                "Is the given feature flag enabled?",
                (_, args, _) =>
                    ValueTask.FromResult(args.GetString("flagKey") == "enabled-flag" ? TruthValue.True : TruthValue.False),
                new PredicateArgumentSchema("flagKey", "The flag key to look up.", LiteralKind.String)
            );

        TruthValue enabledResult = await evaluate(
            new TestContext(null),
            Args("flagKey", "enabled-flag"),
            CancellationToken.None
        );
        TruthValue disabledResult = await evaluate(
            new TestContext(null),
            Args("flagKey", "other-flag"),
            CancellationToken.None
        );

        Assert.Equal(TruthValue.True, enabledResult);
        Assert.Equal(TruthValue.False, disabledResult);
        Assert.Equal("isFeatureEnabled", schema.Name);
    }

    [Fact]
    public async Task Create_WithResolveAndTest_ResolveThrows_ExceptionSurfacesUnwrapped()
    {
        InvalidOperationException expected = new("lookup failed");
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            ResolvedValuePredicates.Create<TestContext, decimal>(
                "isWithinBudget",
                "Is Within Budget",
                "Is the amount within the resolved limit?",
                (_, _, _) => throw expected,
                static _ => TruthValue.True,
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
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> first) =
            ResolvedValuePredicates.Create<TestContext, string>(
                "sameName",
                "Same Name",
                "Same schema, different closures.",
                (_, _, _) => ValueTask.FromResult("first"),
                static resolved => resolved == "first" ? TruthValue.True : TruthValue.False
            );
        (_, Func<TestContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> second) =
            ResolvedValuePredicates.Create<TestContext, string>(
                "sameName",
                "Same Name",
                "Same schema, different closures.",
                (_, _, _) => ValueTask.FromResult("second"),
                static resolved => resolved == "second" ? TruthValue.True : TruthValue.False
            );

        TruthValue firstResult = await first(new TestContext(null), PredicateArguments.Empty, CancellationToken.None);
        TruthValue secondResult = await second(new TestContext(null), PredicateArguments.Empty, CancellationToken.None);

        Assert.Equal(TruthValue.True, firstResult);
        Assert.Equal(TruthValue.True, secondResult);
    }

    private static PredicateArguments Args(string name, string value)
    {
        return new PredicateArguments(new Dictionary<string, LiteralValue> { [name] = LiteralValue.OfString(value) });
    }
}
