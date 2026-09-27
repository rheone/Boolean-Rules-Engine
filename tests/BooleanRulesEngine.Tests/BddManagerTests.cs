namespace BooleanRulesEngine.Tests;

using BooleanRulesEngine.Analysis;

/// <summary>Ticket 02: direct unit tests for the hand-rolled ROBDD engine.</summary>
public sealed class BddManagerTests
{
    [Fact]
    public void Variable_returns_a_stable_node_id_on_repeated_calls()
    {
        BddManager bdd = new();

        int first = bdd.Variable(0);
        int second = bdd.Variable(0);

        Assert.Equal(first, second);
    }

    [Fact]
    public void And_of_a_variable_and_its_negation_reduces_to_false()
    {
        BddManager bdd = new();
        int x = bdd.Variable(0);

        int result = bdd.And(x, bdd.Not(x));

        Assert.Equal(BddManager.False, result);
    }

    [Fact]
    public void Or_of_a_variable_and_its_negation_reduces_to_true()
    {
        BddManager bdd = new();
        int x = bdd.Variable(0);

        int result = bdd.Or(x, bdd.Not(x));

        Assert.Equal(BddManager.True, result);
    }

    [Fact]
    public void And_of_a_variable_with_itself_reduces_to_itself()
    {
        BddManager bdd = new();
        int x = bdd.Variable(0);

        int result = bdd.And(x, x);

        Assert.Equal(x, result);
    }

    [Fact]
    public void Or_of_a_variable_with_itself_reduces_to_itself()
    {
        BddManager bdd = new();
        int x = bdd.Variable(0);

        int result = bdd.Or(x, x);

        Assert.Equal(x, result);
    }

    [Fact]
    public void Structurally_equal_bdds_built_independently_share_the_same_node_id()
    {
        BddManager bdd = new();

        int first = bdd.And(bdd.Variable(0), bdd.Variable(1));
        int second = bdd.And(bdd.Variable(0), bdd.Variable(1));

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    public void Xor_matches_its_truth_table(bool a, bool b, bool expected)
    {
        BddManager bdd = new();
        int x = a ? BddManager.True : BddManager.False;
        int y = b ? BddManager.True : BddManager.False;

        int result = bdd.Xor(x, y);

        Assert.Equal(expected ? BddManager.True : BddManager.False, result);
    }

    [Fact]
    public void Ite_over_three_independent_variables_matches_its_and_or_not_expansion()
    {
        // BddManager.Ite(i, t, e) is defined semantically as "(i AND t) OR (NOT i AND e)". Because
        // ROBDD nodes are uniquified per (variable, low, high) under a fixed variable ordering, two
        // different constructions of the *same* boolean function are guaranteed to produce the
        // identical node id — so comparing node ids here is equivalent to checking every one of the
        // 8 possible assignments of i/t/e against the brute-force truth table.
        BddManager bdd = new();
        int i = bdd.Variable(0);
        int t = bdd.Variable(1);
        int e = bdd.Variable(2);

        int viaIte = bdd.Ite(i, t, e);
        int viaExpansion = bdd.Or(bdd.And(i, t), bdd.And(bdd.Not(i), e));

        Assert.Equal(viaExpansion, viaIte);
    }

    [Fact]
    public void Repeated_ite_calls_with_the_same_operands_remain_correct_under_heavy_reuse()
    {
        BddManager bdd = new();
        int x = bdd.Variable(0);
        int y = bdd.Variable(1);
        int z = bdd.Variable(2);

        int first = bdd.Ite(x, y, z);
        int last = first;
        for (int i = 0; i < 1000; i++)
        {
            last = bdd.Ite(x, y, z);
        }

        Assert.Equal(first, last);

        int expansion = bdd.Or(bdd.And(x, y), bdd.And(bdd.Not(x), z));
        Assert.Equal(expansion, last);
    }
}
