namespace TruthWeaver.Tests;

using TruthWeaver.Ast;

/// <summary>
/// The shared op-name lookup (<see cref="TreeFormatOpNames"/>) that the JSON and YAML tree printers
/// and parsers all consume instead of each maintaining its own copy of the operator ⇄ tree-format
/// string table.
/// </summary>
public sealed class TreeFormatOpNamesTests
{
    [Theory]
    [InlineData("Not", "not")]
    [InlineData("And", "and")]
    [InlineData("Or", "or")]
    [InlineData("Xor", "xor")]
    [InlineData("Equivalent", "equivalent")]
    [InlineData("Implies", "implies")]
    [InlineData("Nand", "nand")]
    [InlineData("Nor", "nor")]
    [InlineData("Nxor", "nxor")]
    [InlineData("Any", "any")]
    [InlineData("All", "all")]
    [InlineData("None", "none")]
    [InlineData("Between", "between")]
    [InlineData("Coalesce", "coalesce")]
    [InlineData("If", "if")]
    [InlineData("IsTrue", "isTrue")]
    [InlineData("IsFalse", "isFalse")]
    [InlineData("IsUnknown", "isUnknown")]
    [InlineData("IsKnown", "isKnown")]
    [InlineData("Project", "project")]
    [InlineData("ExactlyOne", "exactlyOne")]
    [InlineData("AtLeast", "atLeast")]
    [InlineData("AtMost", "atMost")]
    [InlineData("GreaterThan", "greaterThan")]
    [InlineData("LessThan", "lessThan")]
    [InlineData("Exactly", "exactly")]
    public void ToTreeFormat_maps_every_canonical_op_name_to_its_tree_format_string(string opName, string expected)
    {
        Assert.Equal(expected, TreeFormatOpNames.ToTreeFormat(opName));
    }

    [Fact]
    public void ToTreeFormat_throws_for_an_unrecognized_op_name()
    {
        Assert.Throws<InvalidOperationException>(() => TreeFormatOpNames.ToTreeFormat("Bogus"));
    }

    [Theory]
    [InlineData("not", "Not")]
    [InlineData("and", "And")]
    [InlineData("or", "Or")]
    [InlineData("xor", "Xor")]
    [InlineData("equivalent", "Equivalent")]
    [InlineData("xnor", "Equivalent")]
    [InlineData("iff", "Equivalent")]
    [InlineData("IFF", "Equivalent")]
    [InlineData("implies", "Implies")]
    [InlineData("nand", "Nand")]
    [InlineData("NOR", "Nor")]
    [InlineData("IMPLIES", "Implies")]
    [InlineData("nxor", "Nxor")]
    [InlineData("NXOR", "Nxor")]
    [InlineData("any", "Any")]
    [InlineData("ALL", "All")]
    [InlineData("None", "None")]
    [InlineData("between", "Between")]
    [InlineData("COALESCE", "Coalesce")]
    [InlineData("IF", "If")]
    [InlineData("istrue", "IsTrue")]
    [InlineData("ISFALSE", "IsFalse")]
    [InlineData("isUnknown", "IsUnknown")]
    [InlineData("ISKNOWN", "IsKnown")]
    [InlineData("PROJECT", "Project")]
    [InlineData("exactlyOne", "ExactlyOne")]
    [InlineData("atLeast", "AtLeast")]
    [InlineData("atMost", "AtMost")]
    [InlineData("greaterThan", "GreaterThan")]
    [InlineData("lessThan", "LessThan")]
    [InlineData("exactly", "Exactly")]
    public void TryFromTreeFormat_resolves_every_tree_format_string_back_to_its_canonical_op_name(
        string formatName,
        string expectedOpName
    )
    {
        bool resolved = TreeFormatOpNames.TryFromTreeFormat(formatName, out string? opName);

        Assert.True(resolved);
        Assert.Equal(expectedOpName, opName);
    }

    [Theory]
    [InlineData("AND")]
    [InlineData("And")]
    [InlineData("aNd")]
    [InlineData("ATLEAST")]
    [InlineData("AtLeast")]
    public void TryFromTreeFormat_is_case_insensitive(string formatName)
    {
        bool resolved = TreeFormatOpNames.TryFromTreeFormat(formatName, out string? opName);

        Assert.True(resolved);
        Assert.NotNull(opName);
    }

    [Fact]
    public void TryFromTreeFormat_returns_false_for_an_unrecognized_format_string()
    {
        bool resolved = TreeFormatOpNames.TryFromTreeFormat("bogus", out string? opName);

        Assert.False(resolved);
        Assert.Null(opName);
    }
}
