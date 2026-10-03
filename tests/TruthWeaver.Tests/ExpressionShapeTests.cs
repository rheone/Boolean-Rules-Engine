namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;

/// <summary>
/// The shared internal seam (<see cref="ExpressionShape.Of"/>) that every consumer walking an
/// <see cref="Expression"/> tree's operator nodes now uses instead of re-deriving op-name/K/operands
/// with its own independent switch.
/// </summary>
public sealed class ExpressionShapeTests
{
    private static readonly TermExpression TermA = new(new TermIdentity("a", []));
    private static readonly TermExpression TermB = new(new TermIdentity("b", []));

    [Fact]
    public void Not_shape_has_a_single_operand_and_no_k()
    {
        NotExpression node = new(TermA);

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Not", shape.OpName);
        Assert.Null(shape.K);
        Assert.Equal([TermA], shape.Operands);
    }

    [Fact]
    public void And_shape_carries_its_operands_in_order()
    {
        AndExpression node = new(new([TermA, TermB]));

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("And", shape.OpName);
        Assert.Null(shape.K);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Fact]
    public void Or_shape_carries_its_operands_in_order()
    {
        OrExpression node = new(new([TermA, TermB]));

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Or", shape.OpName);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Fact]
    public void Xor_shape_exposes_left_then_right_as_operands()
    {
        XorExpression node = new(TermA, TermB);

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Xor", shape.OpName);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Fact]
    public void Equivalent_shape_exposes_left_then_right_as_operands()
    {
        EquivalentExpression node = new(TermA, TermB);

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Equivalent", shape.OpName);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Fact]
    public void Nand_shape_exposes_left_then_right_as_operands()
    {
        NandExpression node = new(TermA, TermB);

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Nand", shape.OpName);
        Assert.Null(shape.K);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Fact]
    public void Nor_shape_exposes_left_then_right_as_operands()
    {
        NorExpression node = new(TermA, TermB);

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Nor", shape.OpName);
        Assert.Null(shape.K);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Fact]
    public void Implies_shape_exposes_antecedent_then_consequent_as_operands()
    {
        ImpliesExpression node = new(TermA, TermB);

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Implies", shape.OpName);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Fact]
    public void Nxor_shape_carries_its_operands_in_order()
    {
        NxorExpression node = new(new([TermA, TermB, TermA]));

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Nxor", shape.OpName);
        Assert.Null(shape.K);
        Assert.Equal([TermA, TermB, TermA], shape.Operands);
    }

    [Fact]
    public void Any_shape_carries_its_operands_in_order()
    {
        AnyExpression node = new(new([TermA, TermB]));

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Any", shape.OpName);
        Assert.Null(shape.K);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Fact]
    public void All_shape_carries_its_operands_in_order()
    {
        AllExpression node = new(new([TermA, TermB]));

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("All", shape.OpName);
        Assert.Null(shape.K);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Fact]
    public void None_shape_carries_its_operands_in_order()
    {
        NoneExpression node = new(new([TermA, TermB]));

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("None", shape.OpName);
        Assert.Null(shape.K);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Fact]
    public void Between_shape_carries_its_bounds_and_operands_in_order()
    {
        BetweenExpression node = new(1, 2, new([TermA, TermB, TermA]));

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Between", shape.OpName);
        Assert.Equal(1, shape.K);
        Assert.Equal(2, shape.Max);
        Assert.Equal([TermA, TermB, TermA], shape.Operands);
    }

    [Fact]
    public void If_shape_carries_condition_then_both_branches_in_order()
    {
        IfExpression node = new(TermA, TermB, TermA);

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("If", shape.OpName);
        Assert.Null(shape.K);
        Assert.Null(shape.Max);
        Assert.Equal([TermA, TermB, TermA], shape.Operands);
    }

    [Theory]
    [InlineData(InspectionKind.IsTrue, "IsTrue")]
    [InlineData(InspectionKind.IsFalse, "IsFalse")]
    [InlineData(InspectionKind.IsUnknown, "IsUnknown")]
    [InlineData(InspectionKind.IsKnown, "IsKnown")]
    public void Inspection_shape_is_named_after_its_kind_and_carries_its_one_operand(InspectionKind kind, string opName)
    {
        InspectionExpression node = new(kind, TermA);

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal(opName, shape.OpName);
        Assert.Null(shape.K);
        Assert.Null(shape.Max);
        Assert.Equal([TermA], shape.Operands);
    }

    [Fact]
    public void Coalesce_shape_carries_its_operands_in_order()
    {
        CoalesceExpression node = new(new([TermA, TermB]));

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("Coalesce", shape.OpName);
        Assert.Null(shape.K);
        Assert.Null(shape.Max);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Fact]
    public void ExactlyOne_shape_carries_its_operands_in_order()
    {
        ExactlyOneExpression node = new(new([TermA, TermB]));

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal("ExactlyOne", shape.OpName);
        Assert.Null(shape.K);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Theory]
    [InlineData(ThresholdComparison.AtLeast)]
    [InlineData(ThresholdComparison.AtMost)]
    [InlineData(ThresholdComparison.GreaterThan)]
    [InlineData(ThresholdComparison.LessThan)]
    [InlineData(ThresholdComparison.Exactly)]
    public void Threshold_shape_op_name_matches_its_comparison_and_carries_k(ThresholdComparison comparison)
    {
        ThresholdExpression node = new(comparison, 2, new([TermA, TermB]));

        NodeShape shape = ExpressionShape.Of(node);

        Assert.Equal(comparison.ToString(), shape.OpName);
        Assert.Equal(2, shape.K);
        Assert.Equal([TermA, TermB], shape.Operands);
    }

    [Fact]
    public void Constant_nodes_have_no_operand_shape()
    {
        ConstantExpression node = new(TruthValue.True);

        Assert.Throws<ArgumentException>(() => ExpressionShape.Of(node));
    }

    [Fact]
    public void Term_nodes_have_no_operand_shape()
    {
        Assert.Throws<ArgumentException>(() => ExpressionShape.Of(TermA));
    }
}
