namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>Ticket 09: duplicate predicate name registration is rejected, case-insensitively, for both registration overloads.</summary>
public sealed class PredicateRegistryBuilderTests
{
    [Fact]
    public void Registering_two_class_based_predicates_under_the_same_name_throws()
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .Add<ScopedFlagPredicate>();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => builder.Add<ScopedFlagPredicate>());

        Assert.Contains("already registered", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Registering_two_lambda_predicates_under_the_same_name_throws()
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("alwaysTrue", value: true);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => builder.AddConstant("alwaysTrue", value: false));

        Assert.Contains("already registered", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Registering_the_same_name_with_different_casing_is_treated_as_a_collision()
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("Foo", value: true);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => builder.AddConstant("foo", value: false));

        Assert.Contains("already registered", exception.Message, StringComparison.Ordinal);
    }
}
