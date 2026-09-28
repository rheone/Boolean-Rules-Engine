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

    [Fact]
    public void Equals_returns_true_for_the_same_reference()
    {
        TermIdentity term = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]);

        Assert.True(term.Equals(term));
    }

    [Fact]
    public void Equality_operator_returns_true_for_equal_identities()
    {
        TermIdentity first = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]);
        TermIdentity second = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]);

        Assert.True(first == second);
        Assert.False(first != second);
    }

    [Fact]
    public void Equality_operator_returns_false_for_a_different_predicate_name()
    {
        TermIdentity role = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]);
        TermIdentity isManager = new("isManager", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]);

        Assert.False(role == isManager);
        Assert.True(role != isManager);
    }

    [Fact]
    public void Equality_operator_returns_false_for_a_different_argument_value()
    {
        TermIdentity role1 = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]);
        TermIdentity role2 = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Z"))]);

        Assert.False(role1 == role2);
        Assert.True(role1 != role2);
    }

    [Fact]
    public void Equality_operator_returns_false_when_only_one_side_is_null()
    {
        TermIdentity term = new("isManager", []);
        TermIdentity? nullTerm = null;

        Assert.False(term == nullTerm);
        Assert.False(nullTerm == term);
        Assert.True(term != nullTerm);
        Assert.True(nullTerm != term);
    }

    [Fact]
    public void Boxed_equals_returns_true_for_a_boxed_term_identity_with_equal_predicate_name_and_arguments()
    {
        TermIdentity first = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]);
        object second = new TermIdentity(
            "hasRole",
            [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]
        );

        Assert.True(first.Equals(second));
    }

    [Fact]
    public void Boxed_equals_returns_false_for_a_boxed_term_identity_with_a_different_predicate_name()
    {
        TermIdentity role = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]);
        object isManager = new TermIdentity(
            "isManager",
            [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]
        );

        Assert.False(role.Equals(isManager));
    }

    [Fact]
    public void Boxed_equals_returns_false_for_a_boxed_term_identity_with_a_different_argument_value()
    {
        TermIdentity role1 = new("hasRole", [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Y"))]);
        object role2 = new TermIdentity(
            "hasRole",
            [new KeyValuePair<string, LiteralValue>("role", LiteralValue.OfString("Z"))]
        );

        Assert.False(role1.Equals(role2));
    }

    [Fact]
    public void Boxed_equals_returns_false_for_an_unrelated_object()
    {
        TermIdentity term = new("isManager", []);

        Assert.False(term.Equals("isManager"));
    }
}
