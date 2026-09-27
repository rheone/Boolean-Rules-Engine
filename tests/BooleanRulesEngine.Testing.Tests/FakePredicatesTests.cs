namespace BooleanRulesEngine.Testing.Tests;

using BooleanRulesEngine.Abstractions;

public sealed class FakePredicatesTests
{
    [Fact]
    public async Task Returning_Bool_True_AlwaysReturnsTrue()
    {
        (PredicateSchema schema, Func<object?, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) =
            FakePredicates.Returning<object?>("hasRole", true);

        bool result = await evaluate(null, PredicateArguments.Empty, CancellationToken.None);

        Assert.True(result);
        Assert.Equal("hasRole", schema.Name);
    }

    [Fact]
    public async Task Returning_Bool_False_AlwaysReturnsFalse()
    {
        (_, Func<object?, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) = FakePredicates.Returning<object?>(
            "hasRole",
            false
        );

        bool result = await evaluate(null, PredicateArguments.Empty, CancellationToken.None);

        Assert.False(result);
    }

    [Theory]
    [InlineData(TruthValue.True, true)]
    [InlineData(TruthValue.False, false)]
    public async Task Returning_Kleene_TrueOrFalse_ReturnsMatchingBool(TruthValue value, bool expected)
    {
        (_, Func<object?, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) = FakePredicates.Returning<object?>(
            "hasRole",
            value
        );

        bool result = await evaluate(null, PredicateArguments.Empty, CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Fact]
    public Task Returning_Kleene_Unknown_Throws()
    {
        // IPredicate<TContext> only ever returns bool (ADR-0001) - Unknown can only be simulated by
        // throwing, the same way a real predicate signals "cannot determine this".
        (_, Func<object?, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) = FakePredicates.Returning<object?>(
            "hasRole",
            TruthValue.Unknown
        );

        return Assert.ThrowsAsync<SimulatedPredicateFaultException>(async () =>
            await evaluate(null, PredicateArguments.Empty, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Faulting_AlwaysThrowsGivenException()
    {
        InvalidOperationException exception = new("boom");
        (_, Func<object?, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) = FakePredicates.Faulting<object?>(
            "hasRole",
            exception
        );

        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await evaluate(null, PredicateArguments.Empty, CancellationToken.None)
        );
        Assert.Same(exception, thrown);
    }

    [Fact]
    public void Faulting_NullException_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => FakePredicates.Faulting<object?>("hasRole", null!));
    }

    [Fact]
    public async Task Scripted_ReturnsSuccessiveAnswersInOrder()
    {
        (_, Func<object?, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) = FakePredicates.Scripted<object?>(
            "flapping",
            [TruthValue.True, TruthValue.False, TruthValue.True]
        );

        bool first = await evaluate(null, PredicateArguments.Empty, CancellationToken.None);
        bool second = await evaluate(null, PredicateArguments.Empty, CancellationToken.None);
        bool third = await evaluate(null, PredicateArguments.Empty, CancellationToken.None);

        Assert.True(first);
        Assert.False(second);
        Assert.True(third);
    }

    [Fact]
    public async Task Scripted_UnknownEntry_ThrowsOnThatCall()
    {
        (_, Func<object?, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) = FakePredicates.Scripted<object?>(
            "flapping",
            [TruthValue.True, TruthValue.Unknown]
        );

        bool first = await evaluate(null, PredicateArguments.Empty, CancellationToken.None);
        Assert.True(first);

        await Assert.ThrowsAsync<SimulatedPredicateFaultException>(async () =>
            await evaluate(null, PredicateArguments.Empty, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Scripted_MoreCallsThanScriptedAnswers_Throws()
    {
        (_, Func<object?, PredicateArguments, CancellationToken, ValueTask<bool>> evaluate) = FakePredicates.Scripted<object?>(
            "flapping",
            [TruthValue.True]
        );

        await evaluate(null, PredicateArguments.Empty, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await evaluate(null, PredicateArguments.Empty, CancellationToken.None)
        );
    }

    [Fact]
    public void Scripted_EmptyScript_Throws()
    {
        Assert.Throws<ArgumentException>(() => FakePredicates.Scripted<object?>("flapping", []));
    }
}
