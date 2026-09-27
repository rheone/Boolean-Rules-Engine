namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

public sealed class EquatableArrayTests
{
    [Fact]
    public void Arrays_with_the_same_elements_in_the_same_order_are_equal()
    {
        EquatableArray<int> left = new([1, 2, 3]);
        EquatableArray<int> right = new([1, 2, 3]);

        Assert.True(left == right);
        Assert.True(left.Equals(right));
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Arrays_with_the_same_elements_in_a_different_order_are_not_equal()
    {
        EquatableArray<int> left = new([1, 2, 3]);
        EquatableArray<int> right = new([3, 2, 1]);

        Assert.True(left != right);
        Assert.False(left.Equals(right));
    }

    [Fact]
    public void Default_and_explicitly_empty_arrays_are_equal_and_have_zero_count()
    {
        EquatableArray<int> defaultArray = default;
        EquatableArray<int> explicitEmpty = EquatableArray<int>.Empty;

        Assert.Empty(defaultArray);
        Assert.Empty(explicitEmpty);
        Assert.True(defaultArray.Equals(explicitEmpty));
    }

    [Fact]
    public void Indexer_and_enumeration_return_elements_in_source_order()
    {
        EquatableArray<string> array = new(["a", "b", "c"]);

        Assert.Equal("a", array[0]);
        Assert.Equal("c", array[2]);
        Assert.Equal(["a", "b", "c"], array);
    }
}
