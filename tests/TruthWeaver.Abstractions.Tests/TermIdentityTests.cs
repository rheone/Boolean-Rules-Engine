namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

public sealed class TermIdentityTests
{
    [Fact]
    public void Arguments_supplied_in_different_source_order_produce_the_same_identity()
    {
        TermIdentity first = new(
            "hasEnoughApprovals",
            [
                new KeyValuePair<string, LiteralValue>("minCount", LiteralValue.OfInt64(2)),
                new KeyValuePair<string, LiteralValue>("withinHours", LiteralValue.OfInt64(24)),
            ]
        );
        TermIdentity second = new(
            "hasEnoughApprovals",
            [
                new KeyValuePair<string, LiteralValue>("withinHours", LiteralValue.OfInt64(24)),
                new KeyValuePair<string, LiteralValue>("minCount", LiteralValue.OfInt64(2)),
            ]
        );

        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void A_different_argument_value_produces_a_different_identity()
    {
        TermIdentity role1 = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]);
        TermIdentity role2 = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Z"))]);

        Assert.NotEqual(role1, role2);
        Assert.True(role1 != role2);
    }

    [Fact]
    public void Argument_values_compare_case_sensitively()
    {
        TermIdentity lower = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("y"))]);
        TermIdentity upper = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]);

        Assert.NotEqual(lower, upper);
    }

    [Fact]
    public void A_zero_argument_term_renders_as_just_its_predicate_name()
    {
        TermIdentity term = new("isManager", []);

        Assert.Equal("isManager", term.ToString());
    }

    [Fact]
    public void A_term_with_arguments_renders_arguments_sorted_by_name_regardless_of_construction_order()
    {
        TermIdentity term = new(
            "hasEnoughApprovals",
            [
                new KeyValuePair<string, LiteralValue>("withinHours", LiteralValue.OfInt64(24)),
                new KeyValuePair<string, LiteralValue>("minCount", LiteralValue.OfInt64(2)),
            ]
        );

        Assert.Equal("hasEnoughApprovals(minCount: 2, withinHours: 24)", term.ToString());
    }

    [Fact]
    public void Null_is_never_equal_to_a_term_identity()
    {
        TermIdentity term = new("isManager", []);
        TermIdentity? nullTerm = null;

        Assert.False(term.Equals(null));
        Assert.False(term == nullTerm);
        Assert.False(nullTerm == term);
    }
}
